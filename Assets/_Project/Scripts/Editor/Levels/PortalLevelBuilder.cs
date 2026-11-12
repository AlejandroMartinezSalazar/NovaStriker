using System.Text;
using NovaStriker.EditorTools.Setup;
using UnityEditor;
using UnityEngine;

namespace NovaStriker.EditorTools.Levels
{
    /// <summary>
    /// Genera el nivel 13 "Atajo", que presenta los portales.
    /// Idea: una barrera de vidrio irrompible cruza todo el campo. Dos futbolistas están detrás, en el suelo, y no se
    /// ven por encima del vidrio, así que no se puede tirar de frente. La única forma es tirar al portal azul (delante
    /// del vidrio, a la izquierda): el balón sale por el naranja (detrás del vidrio) con la misma velocidad y dirección
    /// y, en diagonal hacia la izquierda, cruza justo por donde están los dos futbolistas, a la altura del pecho.
    /// Un tercer futbolista, subido a dos cajas fijas, asoma por encima del vidrio: se derriba con un tiro normal
    /// (calentamiento y recordatorio de que el vidrio no se rompe).
    /// Medidas comprobadas con el tiro Recto (22 m/s) del nivel 12: apuntando a cualquier punto del disco azul
    /// (±0,5 m del centro), el balón pasa a x 0,8-1,0 / altura 1,1-1,3 m en z = 18 y a x 0,1-0,5 / 0,8-1,2 m en z = 20,5.
    /// </summary>
    internal static class PortalLevelBuilder
    {
        private const string Level12 = LevelPrefabEditor.LevelsFolder + "PF_Level_12.prefab";
        private const string Level13 = LevelPrefabEditor.LevelsFolder + "PF_Level_13.prefab";
        private const float OnTop = LevelPrefabEditor.OnTop;

        private const float BarrierZ = 13f;
        private const int BarrierPanels = 7;     // PF_Glass mide 3 m de ancho: 7 paneles cubren x -10,5 a 10,5 (el suelo mide 20 m).
        private const float GlassCenterY = 1.25f; // PF_Glass mide 2,5 m de alto.

        private static readonly Vector3 PortalA = new Vector3(-3f, 1.5f, 12.4f);
        private static readonly Vector3 PortalB = new Vector3(1.5f, 1.3f, 15.5f);

        [MenuItem("NovaStriker/Niveles/Generar nivel 13 (portales)")]
        private static void Run()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Level12) == null)
            {
                Debug.LogError($"[NovaStriker Niveles] Falta {Level12}: se usa como base del escenario.");
                return;
            }

            bool has13 = AssetDatabase.LoadAssetAtPath<GameObject>(Level13) != null;
            string message = (has13
                                 ? "Se reemplazarán los obstáculos y futbolistas de PF_Level_13."
                                 : "Se creará PF_Level_13 copiando el escenario de PF_Level_12 (no se modifica el 12).") +
                             "\nEl suelo, la cámara y el punto del balón se mantienen.";
            if (!EditorUtility.DisplayDialog("Generar nivel 13", message, "Generar", "Cancelar"))
            {
                return;
            }

            var log = new StringBuilder("[NovaStriker Niveles] Nivel 13 \"Atajo\" (portales)\n");
            if (!PortalBuilder.EnsureAll(log))
            {
                Debug.LogError(log.ToString());
                return;
            }

            if (!has13 && !AssetDatabase.CopyAsset(Level12, Level13))
            {
                Debug.LogError($"{log}- ERROR: no se pudo copiar {Level12} a {Level13}.");
                return;
            }

            if (!LevelPrefabEditor.Rebuild(Level13, BuildAtajo, log))
            {
                Debug.LogError(log.ToString());
                return;
            }

            int problems = LevelIntegrationTool.Integrate(log);
            LevelPrefabEditor.SetDefinition("SO_Level_13", "Atajo", 5, log);
            AssetDatabase.SaveAssets();

            if (problems != 0)
            {
                Debug.LogWarning(log.ToString());
            }
            else
            {
                log.AppendLine("- Pruébalo con NovaStriker/Desarrollo/Probar nivel... (nivel 13).");
                Debug.Log(log.ToString());
            }
        }

        private static void BuildAtajo(LevelPrefabEditor level)
        {
            // Barrera de vidrio irrompible de lado a lado.
            for (int i = 0; i < BarrierPanels; i++)
            {
                float x = (i - (BarrierPanels - 1) * 0.5f) * 3f;
                level.Obstacle(ProjectSetupStage5.GlassPath, new Vector3(x, GlassCenterY, BarrierZ));
            }

            // Portales: azul delante del vidrio (a mano), naranja detrás.
            GameObject portals = level.Obstacle(PortalBuilder.PrefabPath, Vector3.zero);
            portals.transform.Find(PortalBuilder.PortalAName).localPosition = PortalA;
            portals.transform.Find(PortalBuilder.PortalBName).localPosition = PortalB;

            // Dos futbolistas en la línea de salida del portal naranja (solo alcanzables por el portal).
            level.Spawn(new Vector3(0.9f, OnTop, 18f));
            level.Spawn(new Vector3(0.3f, OnTop, 20.5f));

            // Un futbolista sobre dos cajas fijas que asoma por encima del vidrio: tiro directo.
            level.Obstacle(ProjectSetupStage5.StaticBoxPath, new Vector3(5f, 0.5f, 19f));
            level.Obstacle(ProjectSetupStage5.StaticBoxPath, new Vector3(5f, 1.5f, 19f));
            level.Spawn(new Vector3(5f, 2f + OnTop, 19f));
        }
    }
}
