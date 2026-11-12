using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Infrastructure.Online;
using NovaStriker.Infrastructure.Save;
using NUnit.Framework;

namespace NovaStriker.Tests.EditMode.Infrastructure
{
    public class PlayerAccountTests
    {
        private const string Unsupported = "n/a";

        /// <summary>Proveedor manual: guarda las llamadas y se responde desde la prueba.</summary>
        private sealed class ManualProvider : ILoginProvider
        {
            public readonly List<Action<LoginOutcome>> Pending = new List<Action<LoginOutcome>>();
            public readonly List<string> Ids = new List<string>();
            public bool Throw;

            public bool IsAvailable => true;
            public string UnavailableReason => null;

            public void Login(string customId, Action<LoginOutcome> done)
            {
                if (Throw)
                {
                    throw new InvalidOperationException("SDK roto");
                }

                Ids.Add(customId);
                Pending.Add(done);
            }

            public void Succeed(int index, string id = "PF123", bool created = true) => Pending[index](LoginOutcome.Succeeded(id, created));
            public void Fail(int index) => Pending[index](LoginOutcome.Failed("sin red"));
        }

        private sealed class MemoryStorage : ISaveStorage
        {
            public readonly Dictionary<string, string> Data = new Dictionary<string, string>();
            public bool Broken;

            public bool Exists(string key) => Data.ContainsKey(key);

            public string Read(string key)
            {
                if (Broken)
                {
                    throw new System.IO.IOException("disco");
                }

                return Data.TryGetValue(key, out string value) ? value : null;
            }

            public void Write(string key, string data)
            {
                if (Broken)
                {
                    throw new System.IO.IOException("disco");
                }

                Data[key] = data;
            }

            public void Delete(string key) => Data.Remove(key);
        }

        private ManualProvider _provider;
        private EventBus _bus;
        private List<PlayerLoggedInEvent> _logins;

        [SetUp]
        public void SetUp()
        {
            _provider = new ManualProvider();
            _bus = new EventBus();
            _logins = new List<PlayerLoggedInEvent>();
            _bus.Subscribe<PlayerLoggedInEvent>(e => _logins.Add(e));
        }

        [TearDown]
        public void TearDown() => _bus.Dispose();

        private PlayerAccountService Create(float timeout = 10f, float[] retries = null) =>
            new PlayerAccountService(_provider, "device-1", _bus, timeout, retries ?? new[] { 5f, 20f });

        [Test]
        public void Start_LogsInWithDeviceId_AndPublishesNewPlayer()
        {
            PlayerAccountService account = Create();
            var statuses = new List<LoginStatus>();
            account.StatusChanged += statuses.Add;

            account.Start();
            Assert.AreEqual(LoginStatus.InProgress, account.Status, "No bloquea: queda en curso y el juego sigue.");
            Assert.AreEqual("device-1", _provider.Ids[0]);

            _provider.Succeed(0);

            Assert.IsTrue(account.IsLoggedIn);
            Assert.AreEqual("PF123", account.PlayerId);
            Assert.AreEqual(1, _logins.Count);
            Assert.IsTrue(_logins[0].IsNewPlayer);
            CollectionAssert.AreEqual(new[] { LoginStatus.InProgress, LoginStatus.LoggedIn }, statuses);
        }

        [Test]
        public void Failure_GoesOffline_ThenRetriesAfterDelay()
        {
            PlayerAccountService account = Create();
            account.Start();
            _provider.Fail(0);

            Assert.AreEqual(LoginStatus.Offline, account.Status);
            Assert.IsNull(account.PlayerId);

            account.Tick(4.9f);
            Assert.AreEqual(1, account.Attempts);
            account.Tick(0.2f);
            Assert.AreEqual(2, account.Attempts);

            _provider.Succeed(1, created: false);
            Assert.IsTrue(account.IsLoggedIn);
            Assert.IsFalse(_logins[0].IsNewPlayer);
        }

        [Test]
        public void Timeout_CountsAsFailure_ButLateSuccessIsAccepted()
        {
            PlayerAccountService account = Create(timeout: 10f);
            account.Start();

            account.Tick(10f);
            Assert.AreEqual(LoginStatus.Offline, account.Status);

            _provider.Succeed(0);
            Assert.IsTrue(account.IsLoggedIn, "La cuenta ya existe en el servidor: un éxito tardío vale.");
            account.Tick(30f);
            Assert.AreEqual(1, account.Attempts, "Con sesión ya no se reintenta.");
        }

        [Test]
        public void StaleFailure_DoesNotInterruptNewAttempt()
        {
            PlayerAccountService account = Create(timeout: 10f);
            account.Start();
            account.Tick(10f);
            account.Tick(5f);
            Assert.AreEqual(2, account.Attempts);

            _provider.Fail(0);
            Assert.AreEqual(LoginStatus.InProgress, account.Status);
        }

        [Test]
        public void RetriesExhausted_WaitForResume()
        {
            PlayerAccountService account = Create(retries: new[] { 1f });
            account.Start();
            _provider.Fail(0);
            account.Tick(1f);
            _provider.Fail(1);

            account.Tick(1000f);
            Assert.AreEqual(2, account.Attempts, "Sin más reintentos automáticos.");

            account.OnAppResumed();
            Assert.AreEqual(3, account.Attempts);
            Assert.AreEqual(LoginStatus.InProgress, account.Status);
        }

        [Test]
        public void ProviderThrows_GameKeepsGoingOffline()
        {
            _provider.Throw = true;
            PlayerAccountService account = Create();

            Assert.DoesNotThrow(account.Start);
            Assert.AreEqual(LoginStatus.Offline, account.Status);
        }

        [Test]
        public void UnavailableProvider_IsDisabled_AndNeverRetries()
        {
            var account = new PlayerAccountService(new NullLoginProvider("sin SDK"), "device-1", _bus, 10f, new[] { 1f });
            account.Start();
            account.Tick(100f);
            account.OnAppResumed();

            Assert.AreEqual(LoginStatus.Disabled, account.Status);
            Assert.AreEqual(0, account.Attempts);
        }

        [Test]
        public void Resolver_PrefersStored_ThenDevice_ThenNew()
        {
            var storage = new MemoryStorage();

            Assert.AreEqual("abc", PlayerIdResolver.Resolve("abc", Unsupported, storage, () => "new"));
            Assert.AreEqual("abc", storage.Data[PlayerIdResolver.StorageKey]);
            Assert.AreEqual("abc", PlayerIdResolver.Resolve("other", Unsupported, storage, () => "new"), "El guardado manda.");

            Assert.AreEqual("new", PlayerIdResolver.Resolve(Unsupported, Unsupported, new MemoryStorage(), () => "new"));
            Assert.AreEqual("new", PlayerIdResolver.Resolve("0000-0000", Unsupported, new MemoryStorage(), () => "new"));
            Assert.AreEqual("new", PlayerIdResolver.Resolve("  ", Unsupported, new MemoryStorage(), () => "new"));
        }

        [Test]
        public void Resolver_BrokenDisk_StillReturnsId()
        {
            var storage = new MemoryStorage { Broken = true };
            Assert.AreEqual("abc", PlayerIdResolver.Resolve("abc", Unsupported, storage, () => "new"));
        }

        [Test]
        public void Config_DisabledReasons()
        {
            Assert.IsNull(PlayFabConfig.DisabledReason(true, "A1B2C", isEditor: false, loginInEditor: false));
            Assert.IsNotNull(PlayFabConfig.DisabledReason(false, "A1B2C", false, true));
            Assert.IsNotNull(PlayFabConfig.DisabledReason(true, " ", false, true));
            Assert.IsNotNull(PlayFabConfig.DisabledReason(true, "A1B2C", isEditor: true, loginInEditor: false));
        }
    }
}
