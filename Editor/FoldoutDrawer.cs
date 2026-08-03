/*
 * ████████╗██╗   ██╗██████╗  ██████╗ ██████╗ ██╗   ██╗████████╗███████╗
 * ╚══██╔══╝╚██╗ ██╔╝██╔══██╗██╔═══██╗██╔══██╗╚██╗ ██╔╝╚══██╔══╝██╔════╝
 *    ██║    ╚████╔╝ ██████╔╝██║   ██║██████╔╝ ╚████╔╝    ██║   █████╗
 *    ██║     ╚██╔╝  ██╔══██╗██║   ██║██╔══██╗  ╚██╔╝     ██║   ██╔══╝
 *    ██║      ██║   ██║  ██║╚██████╔╝██████╔╝   ██║      ██║   ███████╗
 *    ╚═╝      ╚═╝   ╚═╝  ╚═╝ ╚═════╝ ╚═════╝    ╚═╝      ╚═╝   ╚══════╝
 *
 *    Product  : UIFoldout
 *    Company  : TyroByte Creations
 *    Version  : 1.1.0
 */

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TyroByte
{
    /// <summary>
    /// Handles all IMGUI drawing for a single <see cref="FoldoutGroupCache"/>.
    /// Separated from <see cref="EditorOverride"/> so rendering logic can be
    /// extended or overridden independently of orchestration logic.
    /// </summary>
    internal static class FoldoutDrawer
    {
        // ── Colour tints for each FoldoutColor preset ─────────────────────────
        //    Values are additive overlays on top of the base header background.
        //    Alpha is kept low so they work on both light and dark skins.

        private static readonly Color TintBlue   = new Color(0.10f, 0.30f, 0.80f, 0.28f);
        private static readonly Color TintGreen  = new Color(0.10f, 0.65f, 0.20f, 0.28f);
        private static readonly Color TintRed    = new Color(0.80f, 0.15f, 0.15f, 0.28f);
        private static readonly Color TintYellow = new Color(0.85f, 0.75f, 0.05f, 0.28f);
        private static readonly Color TintTeal   = new Color(0.05f, 0.70f, 0.65f, 0.28f);
        private static readonly Color TintPurple = new Color(0.55f, 0.10f, 0.80f, 0.28f);
        private static readonly Color TintOrange = new Color(0.90f, 0.45f, 0.05f, 0.28f);

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws the header bar + (optionally expanded) property list for
        /// <paramref name="group"/>, then saves the expanded state to EditorPrefs.
        /// </summary>
        /// <param name="group">The group to draw.</param>
        /// <param name="headerStyle">The GUIStyle for the foldout toggle label.</param>
        /// <param name="colors">Theme colours for the current skin.</param>
        /// <param name="prefsKey">EditorPrefs key used to persist expanded state.</param>
        /// <param name="targetObject">The inspected object, passed through to <paramref name="customFieldDrawer"/>.</param>
        /// <param name="customFieldDrawer">
        ///   Optional renderer for <see cref="FoldoutGroupCache.CustomFields"/>. Left null by
        ///   the generic MonoBehaviour/ScriptableObject editor; supplied by specialized
        ///   editors (e.g. a NetworkBehaviour editor) that need to draw fields with no
        ///   usable SerializedProperty representation.
        /// </param>
        public static void Draw(
            FoldoutGroupCache group,
            GUIStyle          headerStyle,
            ThemeColors       colors,
            string            prefsKey,
            Object            targetObject,
            Action<FieldInfo, Object> customFieldDrawer = null)
        {
            // ── Header bar ────────────────────────────────────────────────────
            var headerRect = EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            // Base dark outline
            EditorGUI.DrawRect(
                new Rect(headerRect.x - 1, headerRect.y - 1, headerRect.width + 1, headerRect.height + 1),
                colors.Outline);

            // Base header fill
            EditorGUI.DrawRect(
                new Rect(headerRect.x - 1, headerRect.y - 1, headerRect.width + 1, headerRect.height + 1),
                colors.Header);

            // Optional colour tint overlay
            Color tint = GetTint(group.Attribute.color);
            if (tint.a > 0f)
            {
                EditorGUI.DrawRect(
                    new Rect(headerRect.x - 1, headerRect.y - 1, headerRect.width + 1, headerRect.height + 1),
                    tint);
            }

            // Foldout toggle — save to EditorPrefs on every change
            bool newExpanded = EditorGUILayout.Foldout(
                group.Expanded,
                group.Attribute.name,
                true,
                headerStyle ?? EditorStyles.foldout);

            if (newExpanded != group.Expanded)
            {
                group.Expanded = newExpanded;
                EditorPrefs.SetBool(prefsKey, group.Expanded);
            }

            EditorGUILayout.EndVertical();

            // ── Body (properties) ─────────────────────────────────────────────
            var bodyRect = EditorGUILayout.BeginVertical();

            EditorGUI.DrawRect(
                new Rect(bodyRect.x - 1, bodyRect.y - 1, bodyRect.width + 1, bodyRect.height + 1),
                colors.Body);

            if (group.Expanded)
            {
                // Optionally disable all fields for read-only groups
                EditorGUI.BeginDisabledGroup(group.Attribute.readOnly);

                EditorGUILayout.Space();

                for (int i = 0; i < group.Props.Count; i++)
                {
                    EditorGUI.indentLevel = 1;

                    var prop = group.Props[i];

                    // ── Respect [Space] and [Header] on individual fields ──────
                    //    We query the FieldInfo that owns this property so we can
                    //    read its Unity built-in attributes before drawing it.
                    DrawSpacersFor(prop);

                    EditorGUILayout.PropertyField(
                        prop,
                        new GUIContent(prop.name.FirstLetterToUpperCase()),
                        true);

                    if (i == group.Props.Count - 1 && group.CustomFields.Count == 0)
                        EditorGUILayout.Space();
                }

                // ── Fields with no usable SerializedProperty (e.g. NetworkVariable<T>) ──
                if (customFieldDrawer != null)
                {
                    for (int i = 0; i < group.CustomFields.Count; i++)
                    {
                        var field = group.CustomFields[i];
                        DrawSpacersFor(field);
                        customFieldDrawer(field, targetObject);
                    }

                    if (group.CustomFields.Count > 0)
                        EditorGUILayout.Space();
                }

                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads [Space] and [Header] attributes from the serialized property's
        /// backing field and inserts the appropriate IMGUI layout elements.
        /// </summary>
        private static void DrawSpacersFor(SerializedProperty prop)
        {
            // SerializedProperty does not expose its FieldInfo directly, but
            // we can check the property's attributes via the serialized object's
            // target type.  This is a best-effort approach — it handles the
            // common single-level case (non-nested fields).
            var targetType = prop.serializedObject?.targetObject?.GetType();
            if (targetType == null) return;

            var field = targetType.GetField(
                prop.name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null) return;

            DrawSpacersFor(field);
        }

        /// <summary>
        /// Reads [Space] and [Header] attributes directly off a reflected field
        /// and inserts the appropriate IMGUI layout elements. Shared by both the
        /// SerializedProperty path above and custom (non-serialized) field drawing.
        /// </summary>
        private static void DrawSpacersFor(FieldInfo field)
        {
            // [Space]
            var spaceAttr = System.Attribute.GetCustomAttribute(field, typeof(SpaceAttribute)) as SpaceAttribute;
            if (spaceAttr != null)
                EditorGUILayout.Space(spaceAttr.height);

            // [Header]
            var headerAttr = System.Attribute.GetCustomAttribute(field, typeof(HeaderAttribute)) as HeaderAttribute;
            if (headerAttr != null)
            {
                EditorGUI.indentLevel = 0;
                EditorGUILayout.LabelField(headerAttr.header, EditorStyles.boldLabel);
                EditorGUI.indentLevel = 1;
            }
        }

        private static Color GetTint(FoldoutColor preset)
        {
            return preset switch
            {
                FoldoutColor.Blue   => TintBlue,
                FoldoutColor.Green  => TintGreen,
                FoldoutColor.Red    => TintRed,
                FoldoutColor.Yellow => TintYellow,
                FoldoutColor.Teal   => TintTeal,
                FoldoutColor.Purple => TintPurple,
                FoldoutColor.Orange => TintOrange,
                _                   => Color.clear
            };
        }
    }

    // ── Theme colour container ─────────────────────────────────────────────────

    /// <summary>
    /// Holds the three background colours used to paint foldout sections,
    /// resolved once per skin (light / dark) in <see cref="EditorOverride"/>.
    /// </summary>
    internal struct ThemeColors
    {
        public Color Outline;  // dark border line
        public Color Header;   // header bar fill
        public Color Body;     // expanded body background
    }
}
#endif
