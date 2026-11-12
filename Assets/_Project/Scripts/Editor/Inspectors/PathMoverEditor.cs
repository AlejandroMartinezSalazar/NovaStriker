using NovaStriker.Gameplay.Obstacles;
using UnityEditor;
using UnityEngine;

namespace NovaStriker.EditorTools.Inspectors
{
    /// <summary>
    /// Inspector de PathMover: además de los campos, permite arrastrar los puntos de ruta en la vista de escena
    /// (con deshacer) y muestra su número. Los puntos se guardan como desplazamientos desde la posición inicial.
    /// </summary>
    [CustomEditor(typeof(PathMover))]
    internal sealed class PathMoverEditor : UnityEditor.Editor
    {
        private SerializedProperty _waypoints;

        private void OnEnable()
        {
            _waypoints = serializedObject.FindProperty("_waypoints");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Los puntos son desplazamientos desde la posición inicial del objeto (ejes del mundo). " +
                "Arrástralos en la vista de escena con las flechas.", MessageType.Info);
            DrawDefaultInspector();
        }

        private void OnSceneGUI()
        {
            var mover = (PathMover)target;
            serializedObject.Update();

            Vector3 origin = mover.Origin;
            Vector3 previous = Vector3.zero;
            for (int i = 0; i < _waypoints.arraySize; i++)
            {
                SerializedProperty element = _waypoints.GetArrayElementAtIndex(i);
                Vector3 world = origin + element.vector3Value;

                Handles.Label(world + Vector3.up * 0.3f, $"P{i}");
                if (i > 0)
                {
                    Handles.DrawLine(previous, world);
                }
                previous = world;

                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(world, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    element.vector3Value = moved - origin;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
