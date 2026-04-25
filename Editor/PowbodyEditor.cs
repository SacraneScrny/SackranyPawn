#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using SackranyPawn.Components;
using SackranyPawn.Entities.Modules;

using UnityEditor;
using UnityEngine;

namespace SackranyPawn.Editor
{
    readonly struct DepInfo
    {
        public readonly Type RequiredType;
        public readonly bool IsOptional;
        public readonly bool IsArray;
        public readonly bool IsSatisfied;

        public DepInfo(Type type, bool optional, bool array, bool satisfied)
        {
            RequiredType = type;
            IsOptional = optional;
            IsArray = array;
            IsSatisfied = satisfied;
        }
    }

    readonly struct LimbAnalysis
    {
        public readonly DepInfo[] Deps;
        public readonly int MissingRequired;
        public readonly int MissingOptional;
        public readonly int Satisfied;

        public static readonly LimbAnalysis Empty = new(Array.Empty<DepInfo>(), 0, 0, 0);

        public LimbAnalysis(DepInfo[] deps, int mr, int mo, int sat)
        {
            Deps = deps;
            MissingRequired = mr;
            MissingOptional = mo;
            Satisfied = sat;
        }
    }

    readonly struct BodyAnalysis
    {
        public readonly LimbAnalysis[] Limbs;
        public readonly int TotalMR, TotalMO, TotalSat;

        public BodyAnalysis(LimbAnalysis[] limbs, int mr, int mo, int sat)
        {
            Limbs = limbs;
            TotalMR = mr;
            TotalMO = mo;
            TotalSat = sat;
        }
    }

    [InitializeOnLoad]
    static class DepCache
    {
        static readonly Dictionary<Type, (Type type, bool optional, bool isArray)[]> _raw = new();

        static DepCache()
        {
            AssemblyReloadEvents.beforeAssemblyReload += () => _raw.Clear();
        }

        public static LimbAnalysis Analyze(Type limbType, List<Type> presentLimbTypes, Pawn pawn)
        {
            if (limbType == null) return LimbAnalysis.Empty;

            var raw = GetRaw(limbType);
            var deps = new DepInfo[raw.Length];
            int mr = 0, mo = 0, sat = 0;

            for (int i = 0; i < raw.Length; i++)
            {
                var (reqType, optional, isArray) = raw[i];
                bool found = IsSatisfied(reqType, presentLimbTypes, pawn);
                deps[i] = new DepInfo(reqType, optional, isArray, found);

                if (found) sat++;
                else if (optional) mo++;
                else mr++;
            }

            return new LimbAnalysis(deps, mr, mo, sat);
        }

        static (Type type, bool optional, bool isArray)[] GetRaw(Type limbType)
        {
            if (_raw.TryGetValue(limbType, out var cached)) return cached;

            var list = new List<(Type, bool, bool)>();
            var current = limbType;

            while (current != null && current != typeof(object))
            {
                foreach (var fi in current.GetFields(
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    var attr = fi.GetCustomAttribute<DependencyAttribute>();
                    if (attr == null) continue;
                    bool isArray = fi.FieldType.IsArray;
                    var elemType = isArray ? fi.FieldType.GetElementType() : fi.FieldType;
                    list.Add((elemType, attr.Optional, isArray));
                }
                current = current.BaseType;
            }

            var result = list.ToArray();
            _raw[limbType] = result;
            return result;
        }

        static bool IsSatisfied(Type required, List<Type> presentLimbTypes, Pawn pawn)
        {
            foreach (var t in presentLimbTypes)
                if (required.IsAssignableFrom(t)) return true;

            if (pawn != null && typeof(Component).IsAssignableFrom(required))
            {
                if (pawn.GetComponent(required) != null) return true;
                if (pawn.GetComponentInChildren(required, true) != null) return true;
            }

            return false;
        }
    }

    [InitializeOnLoad]
    static class LimbTypeCache
    {
        public static List<Type> Types { get; private set; }

        static LimbTypeCache()
        {
            Build();
            AssemblyReloadEvents.afterAssemblyReload += Build;
        }

        static void Build()
        {
            Types = TypeCache.GetTypesDerivedFrom<Limb>()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .ToList();
        }
    }

    [CustomEditor(typeof(Pawn))]
    public sealed class PawnBodyEditor : UnityEditor.Editor
    {
        static readonly Color ColRed = new(0.95f, 0.35f, 0.35f);
        static readonly Color ColYellow = new(1f, 0.82f, 0.25f);
        static readonly Color ColGreen = new(0.35f, 0.82f, 0.4f);
        static readonly Color ColBgDark = new(0.18f, 0.18f, 0.18f);
        static readonly Color ColBgLight = new(0.78f, 0.78f, 0.78f);
        static readonly Color ColDepDark = new(0.14f, 0.14f, 0.14f);
        static readonly Color ColDepLight = new(0.72f, 0.72f, 0.72f);

        static GUIStyle _headerStyle;
        static GUIStyle HeaderStyle()
        {
            return _headerStyle ??= new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11,
            };
        }

        readonly Dictionary<int, bool> _foldouts = new();
        readonly Dictionary<int, bool> _debugModes = new();

        SerializedProperty _bodyProp;
        SerializedProperty _limbsProp;
        SerializedProperty _modeProp;

        void OnEnable()
        {
            _foldouts.Clear();
            _debugModes.Clear();
            _bodyProp = serializedObject.FindProperty("Body");
            _limbsProp = _bodyProp?.FindPropertyRelative("Limbs");
            _modeProp = _bodyProp?.FindPropertyRelative("Mode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPawnFields();
            EditorGUILayout.Space(6);

            if (_limbsProp != null)
                DrawBody();
            else
                EditorGUILayout.HelpBox("Body serialized property not found.", MessageType.Error);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawPawnFields()
        {
            var it = serializedObject.GetIterator();
            it.NextVisible(true);
            do
            {
                if (it.name == "Body") continue;
                using (new EditorGUI.DisabledScope(it.name == "m_Script"))
                    EditorGUILayout.PropertyField(it, true);
            }
            while (it.NextVisible(false));
        }

        void DrawBody()
        {
            var pawn = (Pawn)target;
            var analysis = BuildBodyAnalysis(pawn);

            DrawBodyHeader(analysis);
            EditorGUILayout.Space(2);

            int removeAt = -1;
            int swapA = -1, swapB = -1;

            for (int i = 0; i < _limbsProp.arraySize; i++)
            {
                var cmd = DrawLimb(i, analysis, pawn);
                if (cmd == LimbCmd.Remove) removeAt = i;
                else if (cmd == LimbCmd.MoveUp) { swapA = i - 1; swapB = i; }
                else if (cmd == LimbCmd.MoveDown) { swapA = i;     swapB = i + 1; }
            }

            if (removeAt >= 0)
            {
                ApplyRemove(removeAt);
                serializedObject.ApplyModifiedProperties();
            }
            else if (swapA >= 0 && swapB < _limbsProp.arraySize)
            {
                _limbsProp.MoveArrayElement(swapA, swapB);
                SwapDict(_foldouts, swapA, swapB);
                SwapDict(_debugModes, swapA, swapB);
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ Add Limb"))
                ShowAddMenu();
        }

        void DrawBodyHeader(BodyAnalysis a)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Body", EditorStyles.boldLabel, GUILayout.Width(36));

                if (_modeProp != null)
                    EditorGUILayout.PropertyField(_modeProp, GUIContent.none, GUILayout.Width(160));

                GUILayout.FlexibleSpace();
                ToolbarBadge(a.TotalMR, ColRed, "●");
                GUILayout.Space(2);
                ToolbarBadge(a.TotalMO, ColYellow, "◐");
                GUILayout.Space(2);
                ToolbarBadge(a.TotalSat, ColGreen, "✓");
                GUILayout.Space(4);
            }
        }

        enum LimbCmd { None, Remove, MoveUp, MoveDown }

        LimbCmd DrawLimb(int idx, BodyAnalysis analysis, Pawn pawn)
        {
            var elemProp = _limbsProp.GetArrayElementAtIndex(idx);
            var limbType = elemProp.managedReferenceValue?.GetType();
            var la = idx < analysis.Limbs.Length ? analysis.Limbs[idx] : LimbAnalysis.Empty;
            string label = limbType != null ? ObjectNames.NicifyVariableName(limbType.Name) : "(None)";
            bool isDark = EditorGUIUtility.isProSkin;

            float rowH = EditorGUIUtility.singleLineHeight + 6f;
            var rowRect = EditorGUILayout.GetControlRect(false, rowH);
            EditorGUI.DrawRect(rowRect, isDark ? ColBgDark : ColBgLight);

            var cmd = LimbCmd.None;

            var xRect = SlotRight(ref rowRect, 22f);
            var downRect = SlotRight(ref rowRect, 22f);
            var upRect = SlotRight(ref rowRect, 22f);
            var dbgRect = SlotRight(ref rowRect, 46f);

            if (GUI.Button(xRect, "×", EditorStyles.miniButtonRight))
                cmd = LimbCmd.Remove;

            using (new EditorGUI.DisabledScope(idx >= _limbsProp.arraySize - 1))
                if (GUI.Button(downRect, "▼", EditorStyles.miniButtonMid))
                    cmd = LimbCmd.MoveDown;

            using (new EditorGUI.DisabledScope(idx == 0))
                if (GUI.Button(upRect, "▲", EditorStyles.miniButtonMid))
                    cmd = LimbCmd.MoveUp;

            _debugModes.TryGetValue(idx, out bool dbg);
            bool newDbg = GUI.Toggle(dbgRect, dbg, "Debug", EditorStyles.miniButtonLeft);
            if (newDbg != dbg) _debugModes[idx] = newDbg;

            BadgeRight(ref rowRect, la.MissingRequired, ColRed, "●");
            BadgeRight(ref rowRect, la.MissingOptional, ColYellow, "◐");
            BadgeRight(ref rowRect, la.Satisfied, ColGreen, "✓");

            _foldouts.TryGetValue(idx, out bool open);
            var foldRect = new Rect(rowRect.x + 4, rowRect.y + 1, rowRect.width - 4, rowRect.height - 2);
            bool newOpen = EditorGUI.Foldout(foldRect, open, label, true, HeaderStyle());
            if (newOpen != open) _foldouts[idx] = newOpen;

            if (!newOpen) return cmd;

            var contentRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(contentRect, isDark
                ? new Color(0.16f, 0.16f, 0.16f)
                : new Color(0.80f, 0.80f, 0.80f));
            EditorGUI.indentLevel++;

            if (la.Deps.Length > 0)
            {
                EditorGUILayout.Space(3);
                DrawDepsBox(la);
                EditorGUILayout.Space(3);
            }

            DrawLimbFields(elemProp);

            if (newDbg)
            {
                if (Application.isPlaying)
                    DrawDebugFields(limbType, elemProp.managedReferenceValue);
                else
                    EditorGUILayout.HelpBox("Debug fields visible in Play Mode.", MessageType.None);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(3);
            EditorGUILayout.EndVertical();

            return cmd;
        }

        static void DrawDepsBox(LimbAnalysis la)
        {
            bool isDark = EditorGUIUtility.isProSkin;
            float lineH = EditorGUIUtility.singleLineHeight + 1f;
            float boxH = la.Deps.Length * lineH + 6f;
            var boxRect = EditorGUILayout.GetControlRect(false, boxH);

            EditorGUI.DrawRect(boxRect, isDark ? ColDepDark : ColDepLight);

            float y = boxRect.y + 3f;
            float x = EditorGUI.indentLevel * 15f + boxRect.x + 6f;
            float w = boxRect.width - x + boxRect.x - 4f;

            foreach (var dep in la.Deps)
            {
                Color c;
                string icon;

                if (dep.IsSatisfied) { c = ColGreen; icon = "✓"; }
                else if (dep.IsOptional) { c = ColYellow; icon = "◐"; }
                else { c = ColRed; icon = "●"; }

                string name = dep.IsArray ? dep.RequiredType.Name + "[]" : dep.RequiredType.Name;
                string text = dep.IsOptional ? $"{icon}  {name}  (optional)" : $"{icon}  {name}";

                var s = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = c } };
                EditorGUI.LabelField(new Rect(x, y, w, lineH), text, s);
                y += lineH;
            }
        }

        static void DrawLimbFields(SerializedProperty elemProp)
        {
            if (elemProp.managedReferenceValue == null) return;

            var iter = elemProp.Copy();
            var end = elemProp.GetEndProperty();
            bool enter = true;

            while (iter.NextVisible(enter))
            {
                enter = false;
                if (SerializedProperty.EqualContents(iter, end)) break;
                EditorGUILayout.PropertyField(iter, true);
            }
        }

        static void DrawDebugFields(Type limbType, object instance)
        {
            if (instance == null || limbType == null) return;

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("── runtime state ──", EditorStyles.centeredGreyMiniLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                var t = limbType;
                while (t != null && t != typeof(object))
                {
                    foreach (var fi in t.GetFields(
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if (fi.Name.StartsWith("<")) continue;
                        if (fi.GetCustomAttribute<SerializeField>() != null) continue;
                        if (fi.GetCustomAttribute<SerializeReference>() != null) continue;

                        EditorGUILayout.LabelField(
                            ObjectNames.NicifyVariableName(fi.Name),
                            fi.GetValue(instance)?.ToString() ?? "(null)",
                            EditorStyles.miniLabel);
                    }
                    t = t.BaseType;
                }
            }
        }

        void ShowAddMenu()
        {
            var menu = new GenericMenu();
            foreach (var type in LimbTypeCache.Types)
            {
                var captured = type;
                menu.AddItem(new GUIContent(type.FullName.Replace('.', '/')), false, () =>
                {
                    serializedObject.Update();
                    int idx = _limbsProp.arraySize;
                    _limbsProp.InsertArrayElementAtIndex(idx);
                    _limbsProp.GetArrayElementAtIndex(idx).managedReferenceValue =
                        Activator.CreateInstance(captured);
                    _foldouts[idx] = true;
                    serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        void ApplyRemove(int idx)
        {
            if (Application.isPlaying)
            {
                var type = _limbsProp.GetArrayElementAtIndex(idx)
                    .managedReferenceValue?.GetType();
                if (type != null)
                {
                    ((Pawn)target).Remove(type);
                    serializedObject.Update();
                    return;
                }
            }
            _limbsProp.DeleteArrayElementAtIndex(idx);
        }

        BodyAnalysis BuildBodyAnalysis(Pawn pawn)
        {
            int n = _limbsProp.arraySize;

            var presentTypes = new List<Type>(n);
            for (int i = 0; i < n; i++)
            {
                var t = _limbsProp.GetArrayElementAtIndex(i).managedReferenceValue?.GetType();
                if (t != null) presentTypes.Add(t);
            }

            var limbs = new LimbAnalysis[n];
            int mr = 0, mo = 0, sat = 0;

            for (int i = 0; i < n; i++)
            {
                var t = _limbsProp.GetArrayElementAtIndex(i).managedReferenceValue?.GetType();
                var la = DepCache.Analyze(t, presentTypes, pawn);
                limbs[i] = la;
                mr += la.MissingRequired;
                mo += la.MissingOptional;
                sat += la.Satisfied;
            }

            return new BodyAnalysis(limbs, mr, mo, sat);
        }

        static void ToolbarBadge(int count, Color color, string icon)
        {
            var s = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = color } };
            GUILayout.Label($"{icon} {count}", s, GUILayout.Width(30));
        }

        static Rect SlotRight(ref Rect row, float width)
        {
            const float gap = 2f;
            var slot = new Rect(row.xMax - width - gap, row.y + 2f, width, row.height - 4f);
            row = new Rect(row.x, row.y, row.width - width - gap * 2f, row.height);
            return slot;
        }

        static void BadgeRight(ref Rect row, int count, Color color, string icon)
        {
            var r = SlotRight(ref row, 28f);
            var s = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = color } };
            EditorGUI.LabelField(r, $"{icon}{count}", s);
        }

        static void SwapDict<T>(Dictionary<int, T> d, int a, int b)
        {
            d.TryGetValue(a, out var va);
            d.TryGetValue(b, out var vb);
            d[a] = vb;
            d[b] = va;
        }
    }
}
#endif