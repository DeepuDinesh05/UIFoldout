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
    /// Marks the end of a foldEverything group.
    ///
    /// When a <see cref="FoldoutAttribute"/> is declared with foldEverything = true,
    /// every subsequent field is pulled into that group automatically.
    /// Place [FoldoutEnd] on the first field that should fall OUTSIDE the group.
    ///
    /// Example:
    ///   [Foldout("Settings", true)]
    ///   public float speed;
    ///   public float mass;        // still inside "Settings"
    ///
    ///   [FoldoutEnd]
    ///   public bool debugFlag;    // now lives outside all groups
    ///
    /// If foldEverything is false on the preceding [Foldout], this attribute
    /// has no effect and is silently ignored.
    /// </summary>
    public class FoldoutEndAttribute : PropertyAttribute { }
}
