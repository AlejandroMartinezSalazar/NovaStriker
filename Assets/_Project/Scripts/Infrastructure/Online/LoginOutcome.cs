namespace NovaStriker.Infrastructure.Online
{
    /// <summary>Resultado de un intento de inicio de sesión, independiente del SDK.</summary>
    public readonly struct LoginOutcome
    {
        public readonly bool Success;
        public readonly string PlayerId;
        public readonly bool NewlyCreated;
        public readonly string Error;

        private LoginOutcome(bool success, string playerId, bool newlyCreated, string error)
        {
            Success = success;
            PlayerId = playerId;
            NewlyCreated = newlyCreated;
            Error = error;
        }

        public static LoginOutcome Succeeded(string playerId, bool newlyCreated) => new LoginOutcome(true, playerId, newlyCreated, null);

        public static LoginOutcome Failed(string error) => new LoginOutcome(false, null, false, error);
    }
}
