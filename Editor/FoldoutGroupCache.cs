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
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace TyroByte
{
    /// <summary>
    /// Holds the runtime state for one foldout group during an inspector session.
    /// Separated from <see cref="EditorOverride"/> so the data model is independently
    /// readable and testable.
    /// </summary>
    internal class FoldoutGroupCache
    {
        // ── Attribute that declared this group ────────────────────────────────

        /// <summary>The [Foldout] attribute that created this group.</summary>
        public FoldoutAttribute Attribute;

        // ── Membership ────────────────────────────────────────────────────────

        /// <summary>
        /// Field names (as they appear on the SerializedObject) that belong
        /// to this group.  Populated during the one-time scan in OnInspectorGUI.
        /// </summary>
        public HashSet<string> FieldNames = new HashSet<string>();

        /// <summary>
        /// Serialized properties that map to <see cref="FieldNames"/>, in
        /// declaration order.  Copied from the serialized object iterator.
        /// </summary>
        public List<SerializedProperty> Props = new List<SerializedProperty>();

        /// <summary>
        /// Fields that belong to this group but do not resolve to a usable
        /// <see cref="SerializedProperty"/> (e.g. a wrapper type whose default
        /// Unity drawing would be meaningless, such as Netcode's NetworkVariable).
        /// Left empty unless a specialized Editor subclass populates it and
        /// supplies a custom draw delegate to <see cref="FoldoutDrawer.Draw"/>.
        /// Kept reflection-only here so this file has no dependency on any
        /// specific networking package.
        /// </summary>
        public List<FieldInfo> CustomFields = new List<FieldInfo>();

        // ── UI state ──────────────────────────────────────────────────────────

        /// <summary>Whether this group is currently expanded in the inspector.</summary>
        public bool Expanded;

        // ─────────────────────────────────────────────────────────────────────

        public void Dispose()
        {
            foreach (var p in Props)
                p.Dispose();

            Props.Clear();
            FieldNames.Clear();
            CustomFields.Clear();
            Attribute = null;
        }
    }
}
#endif
