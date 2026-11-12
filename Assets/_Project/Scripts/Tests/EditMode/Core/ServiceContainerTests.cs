using System;
using NovaStriker.Core.DI;
using NUnit.Framework;

namespace NovaStriker.Tests.EditMode.Core
{
    public class ServiceContainerTests
    {
        private interface IServiceA { }
        private interface IServiceB { IServiceA A { get; } }

        private sealed class ServiceA : IServiceA, IDisposable
        {
            public int DisposeCount;
            public void Dispose() => DisposeCount++;
        }

        private sealed class ServiceB : IServiceB
        {
            public ServiceB(IServiceA a) => A = a;
            public IServiceA A { get; }
        }

        private ServiceContainer _container;

        [SetUp]
        public void SetUp() => _container = new ServiceContainer();

        [TearDown]
        public void TearDown() => _container.Dispose();

        [Test]
        public void Resolve_ReturnsRegisteredInstance()
        {
            var a = new ServiceA();
            _container.RegisterInstance<IServiceA>(a);

            Assert.AreSame(a, _container.Resolve<IServiceA>());
        }

        [Test]
        public void Resolve_Missing_ThrowsWithTypeName()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => _container.Resolve<IServiceA>());
            StringAssert.Contains(nameof(IServiceA), ex.Message);
        }

        [Test]
        public void TryResolve_Missing_ReturnsFalse()
        {
            Assert.IsFalse(_container.TryResolve(out IServiceA instance));
            Assert.IsNull(instance);
        }

        [Test]
        public void Register_SameTypeTwice_Throws()
        {
            _container.RegisterInstance<IServiceA>(new ServiceA());
            Assert.Throws<InvalidOperationException>(() => _container.RegisterInstance<IServiceA>(new ServiceA()));
        }

        [Test]
        public void Register_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _container.RegisterInstance<IServiceA>(null));
        }

        [Test]
        public void RegisterLazy_CreatesOnceOnFirstResolve()
        {
            int calls = 0;
            _container.RegisterLazy<IServiceA>(_ => { calls++; return new ServiceA(); });

            Assert.AreEqual(0, calls);
            IServiceA first = _container.Resolve<IServiceA>();
            IServiceA second = _container.Resolve<IServiceA>();

            Assert.AreEqual(1, calls);
            Assert.AreSame(first, second);
        }

        [Test]
        public void RegisterLazy_CanResolveItsDependencies()
        {
            var a = new ServiceA();
            _container.RegisterInstance<IServiceA>(a);
            _container.RegisterLazy<IServiceB>(c => new ServiceB(c.Resolve<IServiceA>()));

            Assert.AreSame(a, _container.Resolve<IServiceB>().A);
        }

        [Test]
        public void RegisterLazy_CircularDependency_Throws()
        {
            _container.RegisterLazy<IServiceA>(c => { c.Resolve<IServiceB>(); return new ServiceA(); });
            _container.RegisterLazy<IServiceB>(c => new ServiceB(c.Resolve<IServiceA>()));

            var ex = Assert.Throws<InvalidOperationException>(() => _container.Resolve<IServiceA>());
            StringAssert.Contains("circular", ex.Message);
        }

        [Test]
        public void Child_ResolvesFromParent_ParentDoesNotSeeChild()
        {
            var a = new ServiceA();
            _container.RegisterInstance<IServiceA>(a);
            IServiceContainer child = _container.CreateChild();
            child.RegisterLazy<IServiceB>(c => new ServiceB(c.Resolve<IServiceA>()));

            Assert.AreSame(a, child.Resolve<IServiceA>());
            Assert.IsTrue(child.IsRegistered<IServiceB>());
            Assert.IsFalse(_container.IsRegistered<IServiceB>());
        }

        [Test]
        public void Child_CanOverrideParentRegistration()
        {
            var parentA = new ServiceA();
            var childA = new ServiceA();
            _container.RegisterInstance<IServiceA>(parentA);
            IServiceContainer child = _container.CreateChild();
            child.RegisterInstance<IServiceA>(childA);

            Assert.AreSame(childA, child.Resolve<IServiceA>());
            Assert.AreSame(parentA, _container.Resolve<IServiceA>());
        }

        [Test]
        public void Dispose_DisposesOnlyOwnedServices()
        {
            var owned = new ServiceA();
            var notOwned = new ServiceA();
            IServiceContainer child = _container.CreateChild();
            _container.RegisterInstance<IServiceA>(owned, disposeWithContainer: true);
            child.RegisterInstance<IServiceA>(notOwned);

            _container.Dispose();

            Assert.AreEqual(1, owned.DisposeCount);
            Assert.AreEqual(0, notOwned.DisposeCount);
        }

        [Test]
        public void Dispose_LazyOwnedService_DisposedOnlyIfCreated()
        {
            var created = new ServiceA();
            _container.RegisterLazy<IServiceA>(_ => created, disposeWithContainer: true);

            _container.Dispose();

            Assert.AreEqual(0, created.DisposeCount, "Nunca se resolvió, así que no debe liberarse.");
        }

        [Test]
        public void Dispose_ParentDisposesChildren()
        {
            var childOwned = new ServiceA();
            IServiceContainer child = _container.CreateChild();
            child.RegisterInstance<IServiceA>(childOwned, disposeWithContainer: true);

            _container.Dispose();

            Assert.AreEqual(1, childOwned.DisposeCount);
            Assert.Throws<ObjectDisposedException>(() => child.Resolve<IServiceA>());
        }

        [Test]
        public void Dispose_Twice_IsSafe()
        {
            var owned = new ServiceA();
            _container.RegisterInstance<IServiceA>(owned, disposeWithContainer: true);

            _container.Dispose();
            _container.Dispose();

            Assert.AreEqual(1, owned.DisposeCount);
        }

        [Test]
        public void DisposedChild_IsDetachedFromParent()
        {
            var owned = new ServiceA();
            IServiceContainer child = _container.CreateChild();
            child.RegisterInstance<IServiceA>(owned, disposeWithContainer: true);

            child.Dispose();
            _container.Dispose();

            Assert.AreEqual(1, owned.DisposeCount);
        }
    }
}
