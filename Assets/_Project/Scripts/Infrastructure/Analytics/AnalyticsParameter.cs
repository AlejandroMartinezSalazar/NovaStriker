namespace NovaStriker.Infrastructure.Analytics
{
    /// <summary>Parámetro de un evento de analíticas, independiente del SDK: un número entero o un texto.</summary>
    public readonly struct AnalyticsParameter
    {
        public readonly string Name;
        public readonly long Number;
        public readonly string Text;

        private AnalyticsParameter(string name, long number, string text)
        {
            Name = name;
            Number = number;
            Text = text;
        }

        public bool IsText => Text != null;

        public static AnalyticsParameter Of(string name, long value) => new AnalyticsParameter(name, value, null);

        public static AnalyticsParameter Of(string name, string value) => new AnalyticsParameter(name, 0, value ?? string.Empty);

        public static AnalyticsParameter Of(string name, bool value) => new AnalyticsParameter(name, value ? 1 : 0, null);

        public override string ToString() => IsText ? $"{Name}={Text}" : $"{Name}={Number}";
    }
}
