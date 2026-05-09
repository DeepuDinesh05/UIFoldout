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

using UnityEngine;

namespace TyroByte
{
    /// <summary>
    /// Colour preset used to tint the header bar of a foldout group.
    /// </summary>
    public enum FoldoutColor
    {
        Default,
        Blue,
        Green,
        Red,
        Yellow,
        Teal,
        Purple,
        Orange
    }

    /// <summary>
    /// Marks the beginning of a named foldout group in the Unity Inspector.
    /// Place this attribute on the first field that should appear inside the group.
    ///
    /// Usage (drop-in replacement — existing code using [Foldout("Name", true)] continues to work):
    ///   [Foldout("My Settings", true)]
    ///   [Foldout("My Settings", true, startExpanded: true)]
    ///   [Foldout("Debug", true, readOnly: true, color: FoldoutColor.Red)]
    /// </summary>
    public class FoldoutAttribute : PropertyAttribute
    {
        // ── Core (matches original API exactly) ──────────────────────────────
        
        /// <summary>Display name shown on the foldout header bar.</summary>
        public readonly string name;

        /// <summary>
        /// When true, all subsequent fields (until a [FoldoutEnd] or the next
        /// [Foldout] attribute) are automatically pulled into this group.
        /// Mirrors the original foldEverything behaviour.
        /// </summary>
        public readonly bool foldEverything;

        // ── New options ───────────────────────────────────────────────────────

        /// <summary>
        /// Whether this group is expanded by default the very first time it is
        /// encountered (before any user interaction or saved EditorPrefs state).
        /// </summary>
        public readonly bool startExpanded;

        /// <summary>
        /// When true the fields inside this group are drawn in a disabled
        /// (read-only) state.  Useful for debug-only groups.
        /// </summary>
        public readonly bool readOnly;

        /// <summary>
        /// Optional colour tint applied to the header bar of this group.
        /// </summary>
        public readonly FoldoutColor color;

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a foldout group.
        /// </summary>
        /// <param name="name">Header label shown on the group bar.</param>
        /// <param name="foldEverything">
        ///   Pull all following fields into this group automatically
        ///   (original behaviour, kept for backwards compatibility).
        /// </param>
        /// <param name="startExpanded">Expand the group by default on first use.</param>
        /// <param name="readOnly">Render the group's fields as non-editable.</param>
        /// <param name="color">Tint colour for the header bar.</param>
        public FoldoutAttribute(
            string name,
            bool   foldEverything = false,
            bool   startExpanded  = false,
            bool   readOnly       = false,
            FoldoutColor color    = FoldoutColor.Default)
        {
            this.name          = name;
            this.foldEverything = foldEverything;
            this.startExpanded  = startExpanded;
            this.readOnly       = readOnly;
            this.color          = color;
        }
    }
}
