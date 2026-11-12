using UnityEngine;

namespace NovaStriker.Infrastructure.Save
{
    /// <summary>ISerializer sobre UnityEngine.JsonUtility (clases [Serializable] con campos públicos).</summary>
    public sealed class JsonUtilitySerializer : ISerializer
    {
        private readonly bool _prettyPrint;

        public JsonUtilitySerializer(bool prettyPrint = true)
        {
            _prettyPrint = prettyPrint;
        }

        public string Serialize<T>(T data) => JsonUtility.ToJson(data, _prettyPrint);

        public T Deserialize<T>(string raw) => JsonUtility.FromJson<T>(raw);
    }
}
