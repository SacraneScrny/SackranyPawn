#if UNITY_EDITOR
using SackranyPawn.Components;
using UnityEditor;
using UnityEngine;

namespace SackranyPawn.Editor
{
    [InitializeOnLoad]
    static class PawnSceneOverlay
    {
        static readonly Color ColWorking = new(0.2f, 1f, 0.3f, 0.92f);
        static readonly Color ColStopped = new(1f, 0.25f, 0.25f, 0.92f);
        static readonly Color ColBg = new(0f, 0f, 0f, 0.55f);

        static GUIStyle _style;

        static PawnSceneOverlay()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static GUIStyle Style()
        {
            if (_style != null) return _style;
            _style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 2, 2),
            };
            return _style;
        }

        static void OnSceneGUI(SceneView view)
        {
            var pawns = Object.FindObjectsByType<Pawn>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Handles.BeginGUI();
            foreach (var pawn in pawns)
            {
                if (pawn == null) continue;

                Vector3 world = pawn.transform.position;
                Vector2 screen = HandleUtility.WorldToGUIPoint(world);

                bool working = Application.isPlaying ? pawn.IsWorking : true;
                Color col = working ? ColWorking : ColStopped;

                string label = pawn.gameObject.name;
                var size = Style().CalcSize(new GUIContent(label));
                var rect = new Rect(
                    screen.x - size.x * 0.5f,
                    screen.y - size.y - 4f,
                    size.x,
                    size.y);

                EditorGUI.DrawRect(rect, ColBg);
                Style().normal.textColor = col;
                GUI.Label(rect, label, Style());
            }
            Handles.EndGUI();
        }
    }
}
#endif