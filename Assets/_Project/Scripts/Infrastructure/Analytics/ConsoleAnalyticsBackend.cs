using System.Collections.Generic;
using UnityEngine;

namespace NovaStriker.Infrastructure.Analytics
{
    /// <summary>Editor: escribe cada evento en la consola (no envía nada) para comprobar qué se registraría.</summary>
    public sealed class ConsoleAnalyticsBackend : IAnalyticsBackend
    {
        private const string Prefix = "[Analytics]";

        public void Initialize()
        {
            Debug.Log($"{Prefix} Editor: los eventos solo se muestran en consola (en Android van a Firebase).");
        }

        public void LogEvent(string name, IReadOnlyList<AnalyticsParameter> parameters)
        {
            var parts = new string[parameters?.Count ?? 0];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parameters[i].ToString();
            }

            Debug.Log($"{Prefix} {name} {{ {string.Join(", ", parts)} }}");
        }

        public void SetUserProperty(string name, string value) => Debug.Log($"{Prefix} propiedad de usuario {name} = {value}");

        public void SetUserId(string userId) => Debug.Log($"{Prefix} user_id = {userId}");
    }
}
