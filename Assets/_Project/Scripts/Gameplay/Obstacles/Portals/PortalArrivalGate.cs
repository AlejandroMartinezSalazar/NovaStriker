namespace NovaStriker.Gameplay.Obstacles.Portals
{
    /// <summary>
    /// El balón aparece dentro del portal de salida, y su trigger avisa de esa "entrada" en el siguiente paso de física.
    /// Esta puerta ignora esa primera entrada del cuerpo que acaba de llegar, solo durante un instante; si el balón
    /// sale y vuelve a entrar más tarde, viaja normalmente. C# puro.
    /// </summary>
    public sealed class PortalArrivalGate
    {
        public const float DefaultWindow = 0.5f;

        private readonly float _window;
        private object _body;
        private float _deadline = float.NegativeInfinity;
        private bool _expecting;

        public PortalArrivalGate(float window = DefaultWindow)
        {
            _window = window;
        }

        public void Expect(object body, float now)
        {
            _body = body;
            _deadline = now + _window;
            _expecting = true;
        }

        /// <summary>true si esta entrada es la llegada esperada (se ignora una sola vez).</summary>
        public bool ShouldIgnoreEnter(object body, float now)
        {
            if (!_expecting || !ReferenceEquals(body, _body))
            {
                return false;
            }

            _expecting = false;
            _body = null;
            return now <= _deadline;
        }
    }
}
