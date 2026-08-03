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

// UIFoldout ships with no asmdef (see NetworkEditorOverride.cs remarks — intentional,
// for drag-and-drop / unitypackage distribution), so there is no versionDefines
// mechanism to auto-detect Unity Netcode for GameObjects. This script replicates that
// detection by hand: it reflects for the Netcode assembly (no compile-time reference,
// so this file itself needs no scripting-define gate) and offers to toggle
// TYROBYTE_NETCODE_GAMEOBJECTS on for the user instead of asking them to find it in
// Project Settings themselves.
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TyroByte
{
    /// <summary>
    /// Detects whether Unity Netcode for GameObjects is present in the project and,
    /// if so, prompts once to enable <see cref="NetworkEditorOverride"/> by adding the
    /// TYROBYTE_NETCODE_GAMEOBJECTS scripting define symbol. Also strips the define
    /// automatically if Netcode is later removed, so it never lingers as dead state.
    /// </summary>
    [InitializeOnLoad]
    internal static class NetcodeDefineAutoSetup
    {
        private const string DefineSymbol   = "TYROBYTE_NETCODE_GAMEOBJECTS";
        private const string NetcodeMarker  = "Unity.Netcode.NetworkBehaviour, Unity.Netcode.Runtime";
        private const string DeclinedPrefix = "TyroByte_UIFoldout_NetcodePromptDeclined_";

        static NetcodeDefineAutoSetup()
        {
            // Deferred: avoids touching PlayerSettings mid-recompile/mid-import.
            EditorApplication.delayCall += CheckAndSync;
        }

        private static void CheckAndSync()
        {
            var buildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            string current = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
            var    defines = current.Split(';').Where(d => d.Length > 0).ToList();

            bool netcodePresent = Type.GetType(NetcodeMarker) != null;
            bool defineSet      = defines.Contains(DefineSymbol);

            if (netcodePresent && !defineSet)
            {
                if (HasUserDeclined(buildTarget))
                    return;

                bool accepted = EditorUtility.DisplayDialog(
                    "TyroByte UIFoldout",
                    "Unity Netcode for GameObjects was detected in this project.\n\n" +
                    "Enable UIFoldout's network-aware inspector for NetworkBehaviours? " +
                    $"This adds the '{DefineSymbol}' scripting define symbol " +
                    $"for the {buildTarget.TargetName} build target.",
                    "Enable", "Not Now");

                if (accepted)
                {
                    defines.Add(DefineSymbol);
                    PlayerSettings.SetScriptingDefineSymbols(buildTarget, string.Join(";", defines));
                    ClearDeclined(buildTarget);
                }
                else
                {
                    SetDeclined(buildTarget);
                }
            }
            else if (!netcodePresent && defineSet)
            {
                // Netcode was removed from the project — the define is now dead, drop it.
                defines.Remove(DefineSymbol);
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, string.Join(";", defines));
                ClearDeclined(buildTarget);
            }
        }

        // EditorPrefs is machine-wide, not per-project, so the key is salted with the
        // project path to avoid one project's "Not Now" silencing the prompt in another.
        private static string DeclinedKey(NamedBuildTarget target)
            => DeclinedPrefix + Application.dataPath + "_" + target.TargetName;

        private static bool HasUserDeclined(NamedBuildTarget target)
            => EditorPrefs.GetBool(DeclinedKey(target), false);

        private static void SetDeclined(NamedBuildTarget target)
            => EditorPrefs.SetBool(DeclinedKey(target), true);

        private static void ClearDeclined(NamedBuildTarget target)
            => EditorPrefs.DeleteKey(DeclinedKey(target));
    }
}
#endif
