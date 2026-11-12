using System;
using System.Collections.Generic;
using NovaStriker.Contracts.Data;
using NovaStriker.Core.Pooling;
using UnityEngine;

namespace NovaStriker.Gameplay.Ball
{
    /// <summary>
    /// Object Pool de balones (sobre ComponentPool).
    /// - Los balones que salen del escenario vuelven al pool automáticamente.
    /// - Los que se detienen se quedan en la escena como objetos físicos hasta ReleaseAll (reinicio de nivel).
    /// - Si se supera MaxActive, se recicla el más antiguo.
    /// </summary>
    public sealed class BallPool : IBallProvider, IDisposable
    {
        private readonly ComponentPool<BallView> _pool;
        private readonly int _maxActive;
        private readonly LinkedList<BallView> _activeInOrder = new LinkedList<BallView>();

        public BallPool(IBallFactory factory, Transform container, int maxActive)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _maxActive = Mathf.Max(1, maxActive);
            _pool = new ComponentPool<BallView>(() => CreateBall(factory), container, _maxActive, _maxActive);
        }

        public int CountActive => _pool.CountActive;

        public IBall Spawn(Vector3 position)
        {
            while (_activeInOrder.Count >= _maxActive)
            {
                Release(_activeInOrder.First.Value);
            }

            BallView ball = _pool.Get(position, Quaternion.identity);
            _activeInOrder.AddLast(ball);
            return ball;
        }

        public void Prewarm(int count) => _pool.Prewarm(Mathf.Min(count, _maxActive));

        public void ReleaseAll()
        {
            _activeInOrder.Clear();
            _pool.ReleaseAll();
        }

        public void Dispose()
        {
            _activeInOrder.Clear();
            _pool.Dispose();
        }

        private BallView CreateBall(IBallFactory factory)
        {
            BallView ball = factory.Create();
            ball.Settled += OnBallSettled;
            return ball;
        }

        private void OnBallSettled(BallView ball, BallSettleReason reason)
        {
            if (reason == BallSettleReason.OutOfBounds)
            {
                Release(ball);
            }
        }

        private void Release(BallView ball)
        {
            if (_activeInOrder.Remove(ball))
            {
                _pool.Release(ball);
            }
        }
    }
}
