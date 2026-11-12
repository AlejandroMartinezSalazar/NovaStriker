namespace NovaStriker.Infrastructure.Save
{
    /// <summary>Adapter de serialización (hoy JSON con JsonUtility).</summary>
    public interface ISerializer
    {
        string Serialize<T>(T data);

        /// <summary>Lanza una excepción si el texto no es válido.</summary>
        T Deserialize<T>(string raw);
    }
}
