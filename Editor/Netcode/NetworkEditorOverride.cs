/*
 * ████████╗██╗   ██╗██████╗  ██████╗ ██████╗ ██╗   ██╗████████╗███████╗
 * ╚══██╔══╝╚██╗ ██╔╝██╔══██╗██╔═══██╗██╔══██╗╚██╗ ██╔╝╚══██╔══╝██╔════╝
 *    ██║    ╚████╔╝ ██████╔╝██║   ██║██████╔╝ ╚████╔╝    ██║   █████╗
 *    ██║     ╚██╔╝  ██╔══██╗██║   ██║██╔══██╗  ╚██╔╝     ██║   ██╔══╝
 *    ██║      ██║   ██║  ██║╚██████╔╝██████╔╝   ██║      ██║   ███████╗
 *    ╚═╝      ╚═╝   ╚═╝  ╚═╝ ╚═════╝ ╚═════╝    ╚═╝      ╚═╝   ╚══════╝
 *
 *    Company  : TyroByte Creations
 *    Version  : 1.1.0
 */

// This file lives in its own assembly (TyroByte.UIFoldout.Netcode.Editor) with a
// by-name reference to Unity.Netcode.Runtime. That reference only resolves when
// com.unity.netcode.gameobjects is installed, so this assembly is additionally
// gated behind a defineConstraint on TYROBYTE_NETCODE_GAMEOBJECTS in its asmdef —
// when the define isn't set, Unity skips compiling this assembly (and validating
// its references) entirely, so projects without Netcode are completely unaffected.
// Opt in manually once Netcode for GameObjects is installed:
//
//   Project Settings -> Player -> Other Settings -> Scripting Define Symbols
//   Add: TYROBYTE_NETCODE_GAMEOBJECTS
//
#if UNITY_EDITOR && TYROBYTE_NETCODE_GAMEOBJECTS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TyroByte
{
    /// <summary>
    /// Foldout-aware custom editor for <see cref="NetworkBehaviour"/> and its subclasses.
    /// Scans for <see cref="FoldoutAttribute"/> / <see cref="FoldoutEndAttribute"/> exactly
    /// like <see cref="EditorOverride"/>, but additionally understands Netcode's
    /// NetworkVariable/NetworkList fields, which are not drawable via a plain
    /// <see cref="SerializedProperty"/> (see remarks).
    ///
    /// This intentionally supersedes Unity Netcode for GameObjects' own
    /// <c>NetworkBehaviourEditor</c> for every <see cref="NetworkBehaviour"/> in the
    /// project. Unity resolves a project-defined [CustomEditor] over the package's
    /// built-in one, so no further registration is required.
    ///
    /// Serialization issues this editor resolves relative to a naive custom editor:
    ///   1. NetworkVariableBase is [Serializable], so it DOES surface as a
    ///      SerializedProperty — but drawing it with a plain PropertyField renders
    ///      Unity's internal generic-serialization layout, not the value. Fields
    ///      whose type derives NetworkVariableBase are pulled out of the normal
    ///      SerializedProperty pass entirely and drawn via reflection over the
    ///      wrapper's public API instead.
    ///   2. Write access to a NetworkVariable's value depends on its WritePerm and
    ///      the local client's ownership/authority once spawned; drawing it as an
    ///      always-editable field would silently discard edits that the network
    ///      layer will reject. Editability is derived from NetworkVariableBase's
    ///      own CanClientWrite(clientId), not re-implemented locally.
    ///   3. NetworkList<T> has no single value to edit; it is shown read-only with
    ///      an item count, matching Netcode's own inspector behaviour.
    ///   4. A NetworkBehaviour with no NetworkObject on itself or an ancestor is a
    ///      non-functional setup at runtime; this editor surfaces the same warning
    ///      Unity's own editor does instead of silently losing it.
    ///   5. Fields marked [HideInInspector] or [NonSerialized] are excluded from
    ///      NetworkVariable discovery, matching Netcode's fixed (2.7+) behaviour.
    ///
    /// Not supported: multi-object editing (deliberately no [CanEditMultipleObjects] —
    /// NetworkVariable values are read/written directly against a single `target`,
    /// so Unity's mixed-value handling for multi-selection would not apply), and
    /// value types with no built-in field drawer (custom INetworkSerializable
    /// structs, etc.), which fall back to a read-only ToString() label.
    /// </summary>
    [CustomEditor(typeof(NetworkBehaviour), true)]
    public class NetworkEditorOverride : Editor
    {
        // ── State ─────────────────────────────────────────────────────────────

        private readonly Dictionary<string, FoldoutGroupCache> _groupCache
            = new Dictionary<string, FoldoutGroupCache>();

        private readonly List<SerializedProperty> _ungroupedProps
            = new List<SerializedProperty>();

        private readonly List<FieldInfo> _ungroupedNetworkFields
            = new List<FieldInfo>();

        // Names of every recognized NetworkVariableBase-derived field (grouped or
        // not). Used to keep them out of the normal SerializedProperty pass.
        private readonly HashSet<string> _networkFieldNames = new HashSet<string>();

        private List<FieldInfo> _objectFields;
        private bool            _initialized;
        private bool            _hasNetworkObject;
        private ThemeColors     _colors;
        private GUIStyle        _headerStyle;

        private FoldoutAttribute _activeFold;

        private static readonly Action<FieldInfo, Object> DrawCustomFieldDelegate = DrawNetworkField;

        // ── Unity messages ────────────────────────────────────────────────────

        private void Awake()
        {
            BuildHeaderStyle();
        }

        private void OnEnable()
        {
            _initialized = false;

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

            var t        = target.GetType();
            var typeTree = t.GetTypeTree();

            _objectFields = t
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .OrderByDescending(f => typeTree.IndexOf(f.DeclaringType))
                .ToList();

            _hasNetworkObject = CheckForNetworkObject(target as NetworkBehaviour);

            Repaint();
        }

        private void OnDisable()
        {
            foreach (var pair in _groupCache)
                pair.Value.Dispose();

            _groupCache.Clear();
            _ungroupedProps.Clear();
            _ungroupedNetworkFields.Clear();
            _networkFieldNames.Clear();
        }

        // ── Inspector GUI ─────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (!_hasNetworkObject)
            {
                EditorGUILayout.HelpBox(
                    "No NetworkObject component was found on this GameObject or any of its " +
                    "parents. This NetworkBehaviour will not function until one is added.",
                    MessageType.Warning);
                EditorGUILayout.Space();
            }

            if (!_initialized)
            {
                ScanFields();
                MapSerializedProperties();
                _initialized = true;
            }

            // Script field (always first, always disabled)
            if (_ungroupedProps.Count > 0 && _ungroupedProps[0].propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(_ungroupedProps[0], true);

                EditorGUILayout.Space();
            }

            foreach (var pair in _groupCache)
            {
                string prefsKey = BuildPrefsKey(pair.Key);
                FoldoutDrawer.Draw(pair.Value, _headerStyle, _colors, prefsKey, target, DrawCustomFieldDelegate);
            }

            // Ungrouped regular properties (skip the Script field at index 0)
            for (int i = 1; i < _ungroupedProps.Count; i++)
                EditorGUILayout.PropertyField(_ungroupedProps[i], true);

            // Ungrouped NetworkVariable/NetworkList fields
            for (int i = 0; i < _ungroupedNetworkFields.Count; i++)
                DrawNetworkField(_ungroupedNetworkFields[i], target);

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();
        }

        // ── Scan helpers ──────────────────────────────────────────────────────

        private void ScanFields()
        {
            _activeFold = null;

            foreach (var field in _objectFields)
            {
                bool hasEnd = Attribute.IsDefined(field, typeof(FoldoutEndAttribute));
                if (hasEnd && _activeFold != null && _activeFold.foldEverything)
                {
                    _activeFold = null;
                }

                bool isNetworkVar = IsNetworkVariableField(field);
                if (isNetworkVar)
                    _networkFieldNames.Add(field.Name);

                var fold = Attribute.GetCustomAttribute(field, typeof(FoldoutAttribute)) as FoldoutAttribute;

                if (fold != null)
                {
                    _activeFold = fold;
                    EnsureGroup(fold, field, isNetworkVar);
                    continue;
                }

                if (_activeFold != null && _activeFold.foldEverything)
                {
                    EnsureGroup(_activeFold, field, isNetworkVar);
                }
                else if (isNetworkVar)
                {
                    _ungroupedNetworkFields.Add(field);
                }
                // else: plain ungrouped field, handled by MapSerializedProperties.
            }
        }

        private void EnsureGroup(FoldoutAttribute fold, FieldInfo field, bool isNetworkVar)
        {
            if (!_groupCache.TryGetValue(fold.name, out var cache))
            {
                string prefsKey = BuildPrefsKey(fold.name);
                bool   expanded = EditorPrefs.GetBool(prefsKey, fold.startExpanded);

                cache = new FoldoutGroupCache
                {
                    Attribute = fold,
                    Expanded  = expanded
                };
                _groupCache.Add(fold.name, cache);
            }

            if (isNetworkVar)
                cache.CustomFields.Add(field);
            else
                cache.FieldNames.Add(field.Name);
        }

        private void MapSerializedProperties()
        {
            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // NetworkVariable/NetworkList fields are [Serializable] and would
                // otherwise show up here too — they're drawn separately instead.
                if (_networkFieldNames.Contains(iterator.name))
                    continue;

                bool claimed = false;

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

        private static bool IsNetworkVariableField(FieldInfo field)
        {
            if (!typeof(NetworkVariableBase).IsAssignableFrom(field.FieldType))
                return false;

            if (Attribute.IsDefined(field, typeof(HideInInspector)))
                return false;

            if (field.IsNotSerialized)
                return false;

            return true;
        }

        // ── NetworkObject presence check ─────────────────────────────────────

        /// <summary>
        /// Walks up from this behaviour's transform looking for a NetworkObject,
        /// mirroring Netcode's own nested-NetworkBehaviour support (a NetworkObject
        /// doesn't have to live on the exact same GameObject).
        /// </summary>
        private static bool CheckForNetworkObject(NetworkBehaviour behaviour)
        {
            if (behaviour == null)
                return true;

            var current = behaviour.transform;
            while (current != null)
            {
                if (current.GetComponent<NetworkObject>() != null)
                    return true;

                current = current.parent;
            }

            return false;
        }

        // ── NetworkVariable / NetworkList rendering ──────────────────────────

        /// <summary>
        /// Draws a single NetworkVariableBase-derived field by reflecting over its
        /// public API only (Value / WritePerm / CanClientWrite) — never Netcode's
        /// private internals, so this keeps working across Netcode versions.
        /// </summary>
        private static void DrawNetworkField(FieldInfo field, Object targetObj)
        {
            string label = field.Name.FirstLetterToUpperCase();
            var container = field.GetValue(targetObj) as NetworkVariableBase;

            if (container == null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.LabelField(label, "(not initialized)");
                return;
            }

            var containerType = container.GetType();

            // NetworkList<T> (and anything else collection-shaped) — read-only.
            var countProp = containerType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            if (countProp != null)
            {
                int count = (int)countProp.GetValue(container);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.LabelField(label, $"NetworkList ({count} item{(count == 1 ? "" : "s")})");
                return;
            }

            var valueProp = containerType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            if (valueProp == null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.LabelField(label, container.ToString());
                return;
            }

            var behaviour = targetObj as NetworkBehaviour;
            bool editable = !Application.isPlaying
                || behaviour == null
                || behaviour.NetworkManager == null
                || !behaviour.IsSpawned
                || container.CanClientWrite(behaviour.NetworkManager.LocalClientId);

            object current = valueProp.GetValue(container);

            using (new EditorGUI.DisabledScope(!editable))
            {
                object updated = DrawValueField(label, current);
                if (editable && !Equals(updated, current))
                    valueProp.SetValue(container, updated);
            }

            if (!editable)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(" ", $"Read-only — {container.WritePerm} write permission", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draws a generic value field for common NetworkVariable&lt;T&gt; payload
        /// types. Anything not covered here (custom INetworkSerializable structs,
        /// etc.) falls back to a read-only ToString() label rather than guessing.
        /// </summary>
        private static object DrawValueField(string label, object value)
        {
            switch (value)
            {
                case null:      return null;
                case int i:     return EditorGUILayout.IntField(label, i);
                case long l:    return EditorGUILayout.LongField(label, l);
                case float f:   return EditorGUILayout.FloatField(label, f);
                case double d:  return EditorGUILayout.DoubleField(label, d);
                case bool b:    return EditorGUILayout.Toggle(label, b);
                case string s:  return EditorGUILayout.TextField(label, s);
                case Vector2 v2: return EditorGUILayout.Vector2Field(label, v2);
                case Vector3 v3: return EditorGUILayout.Vector3Field(label, v3);
                case Vector4 v4: return EditorGUILayout.Vector4Field(label, v4);
                case Quaternion q:
                    return Quaternion.Euler(EditorGUILayout.Vector3Field(label, q.eulerAngles));
                case Color c:   return EditorGUILayout.ColorField(label, c);
                case Enum e:    return EditorGUILayout.EnumPopup(label, e);
                default:
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.LabelField(label, value.ToString());
                    return value;
            }
        }

        // ── Style builder ─────────────────────────────────────────────────────

        private void BuildHeaderStyle()
        {
            var texIn   = Resources.Load<Texture2D>("IN foldout focus-6510");
            var texInOn = Resources.Load<Texture2D>("IN foldout focus on-5718");

            _headerStyle = new GUIStyle(EditorStyles.foldout)
            {
                overflow = new RectOffset(-10, 0,  3, 0),
                padding  = new RectOffset( 25, 0, -3, 0)
            };

            if (texIn != null && texInOn != null)
            {
                var white = Color.white;

                _headerStyle.active.textColor    = white;
                _headerStyle.active.background   = texIn;
                _headerStyle.onActive.textColor  = white;
                _headerStyle.onActive.background = texInOn;

                _headerStyle.focused.textColor    = white;
                _headerStyle.focused.background   = texIn;
                _headerStyle.onFocused.textColor  = white;
                _headerStyle.onFocused.background = texInOn;
            }
        }

        // ── Utility ───────────────────────────────────────────────────────────

        private string BuildPrefsKey(string groupName)
            => $"TyroByte_Foldout_{target.GetType().Name}_{groupName}";
    }
}
#endif
