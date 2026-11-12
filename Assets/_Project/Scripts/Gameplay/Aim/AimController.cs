using System;
using NovaStriker.Contracts.Events;
using NovaStriker.Core.Events;
using NovaStriker.Core.Loop;
using NovaStriker.Gameplay.Input;
using NovaStriker.Gameplay.Throwing;
using UnityEngine;

namespace NovaStriker.Gameplay.Aim
{
    /// <summary>
    /// Flujo de apuntado (D4): presionar → arrastrar (preview en vivo) → levantar el dedo = disparar al punto bajo el dedo.
    /// Se activa con Enable/Disable: lo hace AimingState (y se apaga en pausa).
    /// </summary>
    public sealed class AimController : IAimController, IDisposable
    {
        private readonly IPointerInput _input;
        private readonly IUiHitTester _uiHitTester;
        private readonly IAimTargetResolver _targetResolver;
        private readonly IThrowController _throwController;
        private readonly IAimPreview _preview;
        private readonly IEventBus _eventBus;
        private readonly IClock _clock;
        private readonly SwipeGestureTracker _gesture;
        private readonly float _minAimDuration;
        private readonly Vector3[] _trajectoryBuffer;
        private readonly IPhysicsSettings _physics;

        private bool _isEnabled;
        private bool _isAiming;

        public AimController(
            IPointerInput input,
            IUiHitTester uiHitTester,
            IAimTargetResolver targetResolver,
            IThrowController throwController,
            IAimPreview preview,
            IEventBus eventBus,
            IClock clock,
            IPhysicsSettings physics,
            AimSettings settings)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _uiHitTester = uiHitTester ?? throw new ArgumentNullException(nameof(uiHitTester));
            _targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
            _throwController = throwController ?? throw new ArgumentNullException(nameof(throwController));
            _preview = preview ?? throw new ArgumentNullException(nameof(preview));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _physics = physics ?? throw new ArgumentNullException(nameof(physics));
            _gesture = new SwipeGestureTracker(settings.FullCurveRatio, settings.MinChordPixels);
            _minAimDuration = settings.MinAimDuration;
            _trajectoryBuffer = new Vector3[Mathf.Max(2, settings.TrajectorySamples)];
        }

        public bool IsEnabled => _isEnabled;
        public bool IsAiming => _isAiming;

        public void Enable()
        {
            if (_isEnabled)
            {
                return;
            }

            _isEnabled = true;
            _input.Pressed += OnPressed;
            _input.Dragged += OnDragged;
            _input.Released += OnReleased;
        }

        public void Disable()
        {
            if (!_isEnabled)
            {
                return;
            }

            _isEnabled = false;
            _input.Pressed -= OnPressed;
            _input.Dragged -= OnDragged;
            _input.Released -= OnReleased;

            if (_isAiming)
            {
                CancelAim();
            }
        }

        public void Dispose() => Disable();

        private void OnPressed(Vector2 position)
        {
            if (_isAiming || !_throwController.HasBallReady || _uiHitTester.IsOverUi(position))
            {
                return;
            }

            _isAiming = true;
            _gesture.Begin(position, _clock.Time);
            _eventBus.Publish(new AimStartedEvent());
            UpdateAim(position);
        }

        private void OnDragged(Vector2 position)
        {
            if (!_isAiming)
            {
                return;
            }

            _gesture.AddPoint(position);
            UpdateAim(position);
        }

        private void OnReleased(Vector2 position)
        {
            if (!_isAiming)
            {
                return;
            }

            _isAiming = false;
            SwipeData swipe = _gesture.End(position, _clock.Time);
            _preview.Hide();

            bool fired = swipe.Duration >= _minAimDuration
                         && _targetResolver.TryResolve(position, out Vector3 target)
                         && _throwController.TryThrow(target, swipe.CurveBias);

            if (!fired)
            {
                _eventBus.Publish(new AimCancelledEvent());
            }
        }

        private void UpdateAim(Vector2 position)
        {
            float curveBias = _gesture.EvaluateCurveBias(position);

            if (_targetResolver.TryResolve(position, out Vector3 target)
                && _throwController.TryPreview(target, curveBias, out Vector3 origin, out ThrowCommand command))
            {
                int count = BallisticSolver.SampleTrajectory(origin, command, _physics.Gravity, _physics.FixedDeltaTime, _trajectoryBuffer);
                _preview.Show(_trajectoryBuffer, count, target);
                _eventBus.Publish(new AimUpdatedEvent(target, true));
            }
            else
            {
                _preview.Hide();
                _eventBus.Publish(new AimUpdatedEvent(default, false));
            }
        }

        private void CancelAim()
        {
            _isAiming = false;
            _gesture.Cancel();
            _preview.Hide();
            _eventBus.Publish(new AimCancelledEvent());
        }
    }
}
