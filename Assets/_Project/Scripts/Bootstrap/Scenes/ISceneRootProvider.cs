namespace NovaStriker.Bootstrap.Scenes
{
    /// <summary>Localiza la raíz de composición de una escena cargada.</summary>
    internal interface ISceneRootProvider
    {
        bool TryGetRoot(string sceneName, out ISceneCompositionRoot root);
    }
}
