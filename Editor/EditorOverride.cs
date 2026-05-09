/*
 * ████████╗██╗   ██╗██████╗  ██████╗ ██████╗ ██╗   ██╗████████╗███████╗
 * ╚══██╔══╝╚██╗ ██╔╝██╔══██╗██╔═══██╗██╔══██╗╚██╗ ██╔╝╚══██╔══╝██╔════╝
 *    ██║    ╚████╔╝ ██████╔╝██║   ██║██████╔╝ ╚████╔╝    ██║   █████╗
 *    ██║     ╚██╔╝  ██╔══██╗██║   ██║██╔══██╗  ╚██╔╝     ██║   ██╔══╝
 *    ██║      ██║   ██║  ██║╚██████╔╝██████╔╝   ██║      ██║   ███████╗
 *    ╚═╝      ╚═╝   ╚═╝  ╚═╝ ╚═════╝ ╚═════╝    ╚═╝      ╚═╝   ╚══════╝
 *
 *    Company  : TyroByte Creations
 *    Version  : 1.0.0
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TyroByte.EditorTools
{
    /// <summary>
    /// Fallback custom editor for all MonoBehaviours and ScriptableObjects.
    /// Scans for <see cref="FoldoutAttribute"/> and <see cref="FoldoutEndAttribute"/>
    /// on public/serialized fields and groups them into collapsible, styled sections
    /// in the Inspector.
    ///
    /// Responsibilities of this class (orchestration only):
    ///   - Reflect fields and build the <see cref="FoldoutGroupCache"/> dictionary.
    ///   - Persist expanded state via EditorPrefs.
    ///   - Delegate all IMGUI drawing for each group to <see cref="FoldoutDrawer"/>.
    /// </summary>
    [CustomEditor(typeof(Object), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class EditorOverride : Editor
    {
        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>
        /// One cache entry per named foldout group, keyed by the group's display name.
        /// Insertion order is guaranteed (List-backed iteration used below).
        /// </summary>
        private readonly Dictionary<string, FoldoutGroupCache> _groupCache
            = new Dictionary<string, FoldoutGroupCache>();

        /// <summary>Properties that do not belong to any foldout group.</summary>
        private readonly List<SerializedProperty> _ungroupedProps
            = new List<SerializedProperty>();

        private List<FieldInfo> _objectFields;
        private bool            _initialized;
        private ThemeColors     _colors;
        private GUIStyle        _headerStyle;

        // The foldout attribute that is currently "active" during the scan pass.
        // Null once a [FoldoutEnd] is encountered.
        private FoldoutAttribute _activeFold;

        // ── Unity messages ────────────────────────────────────────────────────

        private void Awake()
        {
            BuildHeaderStyle();
        }

        private void OnEnable()
        {
            _initialized = false;

            // Skin-dependent colours
            bool pro = EditorGUIUtility.isProSkin;
            _colors = pro
                ? new ThemeColors
                  {
                      Outline = new Color(0.10f, 0.10f, 0.10f, 1.00f),
                      Header  = new Color(1.00f, 1.00f, 1.00f, 0.10f),
                      Body    = new Color(0.25f, 0.25f, 0.25f, 1.00f)
                  }
                : new ThemeColors
                  {
                      Outline = new Color(0.20f, 0.20f, 0.20f, 1.00f),
                      Header  = new Color(1.00f, 1.00f, 1.00f, 0.55f),
                      Body    = new Color(0.70f, 0.70f, 0.70f, 1.00f)
                  };

            // Reflect fields ordered from base class → most-derived
            var t        = target.GetType();
            var typeTree = t.GetTypeTree();

            _objectFields = t
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .OrderByDescending(f => typeTree.IndexOf(f.DeclaringType))
                .ToList();

            Repaint();
        }

        private void OnDisable()
        {
            foreach (var pair in _groupCache)
                pair.Value.Dispose();

            _groupCache.Clear();
            _ungroupedProps.Clear();
        }

        // ── Inspector GUI ─────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // One-time scan: build group cache and map serialized properties.
            if (!_initialized)
            {
                ScanFields();
                MapSerializedProperties();

                // If nothing ended up in any group, fall back to default.
                if (_groupCache.Count == 0)
                {
                    serializedObject.ApplyModifiedProperties();
                    DrawDefaultInspector();
                    return;
                }

                _initialized = true;
            }

            // Script field (always first, always disabled)
            if (_ungroupedProps.Count > 0 && _ungroupedProps[0].propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(_ungroupedProps[0], true);

                EditorGUILayout.Space();
            }

            // Draw each foldout group via FoldoutDrawer
            foreach (var pair in _groupCache)
            {
                string prefsKey = BuildPrefsKey(pair.Key);
                FoldoutDrawer.Draw(pair.Value, _headerStyle, _colors, prefsKey);
            }

            // Draw any remaining ungrouped properties (skip the Script field at index 0)
            for (int i = 1; i < _ungroupedProps.Count; i++)
                EditorGUILayout.PropertyField(_ungroupedProps[i], true);

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();
        }

        // ── Scan helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Iterates reflected fields and decides which named group (if any)
        /// each field belongs to, respecting [FoldoutEnd] to close foldEverything
        /// groups explicitly.
        /// </summary>
        private void ScanFields()
        {
            _activeFold = null;

            foreach (var field in _objectFields)
            {
                // Check for [FoldoutEnd] — closes any active foldEverything group.
                bool hasEnd = Attribute.IsDefined(field, typeof(FoldoutEndAttribute));
                if (hasEnd && _activeFold != null && _activeFold.foldEverything)
                {
                    _activeFold = null;
                }

                // Check for a new [Foldout] on this field.
                var fold = Attribute.GetCustomAttribute(field, typeof(FoldoutAttribute)) as FoldoutAttribute;

                if (fold != null)
                {
                    // Start a new group (or reuse an existing one with the same name).
                    _activeFold = fold;
                    EnsureGroup(fold, field.Name);
                    continue;
                }

                // No [Foldout] on this field — decide where it goes.
                if (_activeFold != null && _activeFold.foldEverything)
                {
                    // Pull into the active foldEverything group.
                    EnsureGroup(_activeFold, field.Name);
                }
                // else: field stays ungrouped (handled by MapSerializedProperties).
            }
        }

        /// <summary>
        /// Ensures a <see cref="FoldoutGroupCache"/> exists for <paramref name="fold"/>
        /// and registers <paramref name="fieldName"/> as a member.
        /// </summary>
        private void EnsureGroup(FoldoutAttribute fold, string fieldName)
        {
            if (!_groupCache.TryGetValue(fold.name, out var cache))
            {
                // Load persisted expanded state; fall back to startExpanded.
                string prefsKey  = BuildPrefsKey(fold.name);
                bool   expanded  = EditorPrefs.GetBool(prefsKey, fold.startExpanded);

                cache = new FoldoutGroupCache
                {
                    Attribute = fold,
                    Expanded  = expanded
                };
                _groupCache.Add(fold.name, cache);
            }

            cache.FieldNames.Add(fieldName);
        }

        /// <summary>
        /// Iterates serialized properties and assigns each one either to a group
        /// (by matching <see cref="FoldoutGroupCache.FieldNames"/>) or to
        /// <see cref="_ungroupedProps"/>.
        /// </summary>
        private void MapSerializedProperties()
        {
            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                bool claimed  = false;

                foreach (var pair in _groupCache)
                {
                    if (pair.Value.FieldNames.Contains(iterator.name))
                    {
                        pair.Value.Props.Add(iterator.Copy());
                        claimed = true;
                        break;
                    }
                }

                if (!claimed)
                    _ungroupedProps.Add(iterator.Copy());
            }
        }

        // ── Style builder ─────────────────────────────────────────────────────

        /// <summary>
        /// Creates the custom GUIStyle for foldout header labels.
        /// Falls back to the default foldout style if editor textures are unavailable.
        /// </summary>
        private void BuildHeaderStyle()
        {
            var texIn   = Resources.Load<Texture2D>("IN foldout focus-6510");
            var texInOn = Resources.Load<Texture2D>("IN foldout focus on-5718");

            _headerStyle = new GUIStyle(EditorStyles.foldout)
            {
                overflow = new RectOffset(-10, 0,  3, 0),
                padding  = new RectOffset( 25, 0, -3, 0)
            };

            // Only apply custom textures if they were actually found in Resources.
            if (texIn != null && texInOn != null)
            {
                var white = Color.white;

                _headerStyle.active.textColor    = white;
                _headerStyle.active.background   = texIn;
                _headerStyle.onActive.textColor   = white;
                _headerStyle.onActive.background  = texInOn;

                _headerStyle.focused.textColor    = white;
                _headerStyle.focused.background   = texIn;
                _headerStyle.onFocused.textColor  = white;
                _headerStyle.onFocused.background = texInOn;
            }
        }

        // ── Utility ───────────────────────────────────────────────────────────

        /// <summary>
        /// Builds an EditorPrefs key that is unique per target type + group name.
        /// Format: TyroByte_Foldout_{TypeName}_{GroupName}
        /// </summary>
        private string BuildPrefsKey(string groupName)
            => $"TyroByte_Foldout_{target.GetType().Name}_{groupName}";
    }
}
#endif
