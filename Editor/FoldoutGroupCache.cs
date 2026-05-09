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
 *    Version  : 1.0.0
 */

#if UNITY_EDITOR
using System.Collections.Generic;
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
            Attribute = null;
        }
    }
}
#endif
