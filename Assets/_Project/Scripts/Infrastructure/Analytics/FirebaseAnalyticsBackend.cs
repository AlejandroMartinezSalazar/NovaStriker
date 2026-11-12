#if NOVA_FIREBASE
using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using UnityEngine;
using FirebaseAnalyticsApi = Firebase.Analytics.FirebaseAnalytics;
using FirebaseParameter = Firebase.Analytics.Parameter;

namespace NovaStriker.Infrastructure.Analytics
{
    /// <summary>
    /// Adapter de Firebase Analytics. Solo se compila con el símbolo NOVA_FIREBASE (lo activa
    /// NovaStriker/Setup/Firebase - Analíticas). Firebase tarda un momento en arrancar: lo que llega antes
    /// se guarda en una cola corta y se envía al estar listo. Si Firebase no puede arrancar, se descarta todo
    /// y el juego sigue igual.
    /// </summary>
    public sealed class FirebaseAnalyticsBackend : IAnalyticsBackend
    {
        private const int MaxQueued = 100;

        private enum State
        {
            NotStarted,
            Starting,
            Ready,
            Failed,
        }

        private readonly Queue<Action> _queue = new Queue<Action>();
        private State _state = State.NotStarted;

        public void Initialize()
        {
            if (_state != State.NotStarted)
            {
                return;
            }

            _state = State.Starting;
            try
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    bool ok = !task.IsFaulted && !task.IsCanceled && task.Result == DependencyStatus.Available;
                    if (!ok)
                    {
                        string reason = task.IsFaulted ? task.Exception?.GetBaseException().Message : task.IsCanceled ? "cancelado" : task.Result.ToString();
                        Fail(reason);
                        return;
                    }

                    _state = State.Ready;
                    FirebaseAnalyticsApi.SetAnalyticsCollectionEnabled(true);
                    while (_queue.Count > 0)
                    {
                        Run(_queue.Dequeue());
                    }
                });
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
            }
        }

        public void LogEvent(string name, IReadOnlyList<AnalyticsParameter> parameters)
        {
            FirebaseParameter[] converted = Convert(parameters);
            Enqueue(() => FirebaseAnalyticsApi.LogEvent(name, converted));
        }

        public void SetUserProperty(string name, string value) => Enqueue(() => FirebaseAnalyticsApi.SetUserProperty(name, value));

        public void SetUserId(string userId) => Enqueue(() => FirebaseAnalyticsApi.SetUserId(userId));

        private void Enqueue(Action call)
        {
            switch (_state)
            {
                case State.Ready:
                    Run(call);
                    break;
                case State.Failed:
                    break;
                default:
                    if (_queue.Count >= MaxQueued)
                    {
                        _queue.Dequeue();
                    }

                    _queue.Enqueue(call);
                    break;
            }
        }

        private static void Run(Action call)
        {
            try
            {
                call();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NovaStriker] Firebase Analytics: {exception.Message}");
            }
        }

        private void Fail(string reason)
        {
            _state = State.Failed;
            _queue.Clear();
            Debug.LogWarning($"[NovaStriker] Firebase no pudo iniciarse ({reason}): no se enviarán analíticas en esta sesión.");
        }

        private static FirebaseParameter[] Convert(IReadOnlyList<AnalyticsParameter> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return Array.Empty<FirebaseParameter>();
            }

            var result = new FirebaseParameter[parameters.Count];
            for (int i = 0; i < result.Length; i++)
            {
                AnalyticsParameter p = parameters[i];
                result[i] = p.IsText ? new FirebaseParameter(p.Name, p.Text) : new FirebaseParameter(p.Name, p.Number);
            }

            return result;
        }
    }
}
#endif
