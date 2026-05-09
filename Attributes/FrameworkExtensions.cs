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

using System;
using System.Collections.Generic;

namespace TyroByte
{
    /// <summary>
    /// General-purpose extension methods shared across the TyroByte UIFoldout package.
    /// Lives in the Attributes assembly so it is available at both runtime and editor time.
    /// </summary>
    public static partial class FrameworkExtensions
    {
        /// <summary>
        /// Returns the string with its first character converted to upper-case.
        /// Used by the editor drawer to display camelCase field names cleanly.
        /// </summary>
        public static string FirstLetterToUpperCase(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        /// <summary>
        /// Walks the inheritance chain of <paramref name="t"/> and returns
        /// every type from the most-derived down to (but not including) object.
        /// Used to sort reflected fields by declaring class so base-class fields
        /// appear before derived-class fields.
        /// </summary>
        public static IList<Type> GetTypeTree(this Type t)
        {
            var types = new List<Type>();
            while (t.BaseType != null)
            {
                types.Add(t);
                t = t.BaseType;
            }
            return types;
        }
    }
}
