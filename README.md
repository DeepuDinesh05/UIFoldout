# UIFoldout

A lightweight, fully customizable foldout system for the Unity Inspector that helps organize large MonoBehaviour and ScriptableObject inspectors into clean, readable sections.

Built for developers who want better inspector usability without writing custom editors for every component.

---

# Features

- Simple attribute workflow triggered by calling `[Foldout]` attribute
- Works automatically on all `MonoBehaviour` and `ScriptableObject` types
- Persistent foldout state using `EditorPrefs`
- Multiple built-in color presets
- Read-only foldout groups
- `foldEverything` auto-grouping support
- `[FoldoutEnd]` support for explicit group closing
- Supports Unity's built-in `[Header]` and `[Space]`
- Light and dark editor skin aware
  
---

# Installation

## 1. Manual Import

1. Download & Import UnityPackage from releases section.
2. Let Unity recompile

---

## 2. Package Manager

Open: Window → Package Manager

1. Click `+`
2. Select `Add package from git URL`
3. Paste the following url.

```text
https://github.com/DeepuDinesh05/UIFoldout.git
```

---

# Basic Usage

1. Import the namespace:

```csharp
using TyroByte;
```

2. Then create foldout groups:

```csharp
using UnityEngine;
using TyroByte;

public class PlayerController : MonoBehaviour
{
    [Foldout("Movement Settings")]
    public float moveSpeed;

    public float sprintSpeed;

    [Foldout("Combat")]
    public int damage;

    public float attackCooldown;
}
```

---

# foldEverything

Automatically pulls all following fields into the group.

```csharp
[Foldout("Movement Settings", true)]
public float moveSpeed;

public float sprintSpeed;
public float acceleration;
```

The group remains active until:

- Another `[Foldout]`
- Or `[FoldoutEnd]`

---

# FoldoutEnd

Explicitly closes an active `foldEverything` group.

```csharp
[Foldout("Movement Settings", true)]
public float moveSpeed;

public float sprintSpeed;

[FoldoutEnd]
public bool debugFlag;
```

`debugFlag` now exists outside the foldout group.

---

# Color Presets

```csharp
[Foldout("Audio", true, color: FoldoutColor.Green)]
[Foldout("Combat", true, color: FoldoutColor.Red)]
[Foldout("UI", true, color: FoldoutColor.Purple)]
```

Available colors:

- Default
- Blue
- Green
- Red
- Yellow
- Teal
- Purple
- Orange

---

# Start Expanded

Open groups by default on first use.

```csharp
[Foldout("Core Settings", true, startExpanded: true)]
public float speed;
```

Expanded state is then persisted automatically through `EditorPrefs`.

---

# Read Only Groups

Useful for debug data or runtime state visualization.

```csharp
[Foldout("Debug", true, readOnly: true, color: FoldoutColor.Red)]
public int currentState;

public float runtimeTimer;
```

Fields remain visible but become non-editable.

---

# Unity Attribute Support

UIFoldout automatically respects:

- `[Header]`
- `[Space]`

Example:

```csharp
[Foldout("Movement", true)]
public float speed;

[Space(10)]
public float acceleration;

[Header("Advanced")]
public float friction;
```

---

```csharp
using UnityEngine;
using TyroByte;

public class WeaponController : MonoBehaviour
{
    [Foldout(
        "Weapon Settings",
        true,
        startExpanded: true,
        color: FoldoutColor.Blue)]
    [Range(0f, 250f)]
    public float damage = 35f;

    [Range(0.1f, 5f)]
    public float fireRate = 0.25f;

    public bool automaticFire = true;

    [Foldout(
        "Recoil",
        true,
        color: FoldoutColor.Orange)]
    [Range(0f, 10f)]
    public float verticalKick = 2f;

    [Range(0f, 10f)]
    public float horizontalKick = 1f;

    public float recoilRecoverySpeed = 6f;

    [Foldout(
        "Audio",
        true,
        color: FoldoutColor.Green)]
    public AudioClip fireSound;

    public AudioClip reloadSound;

    public float volume = 0.8f;

    [Foldout(
        "Debug Info",
        true,
        readOnly: true,
        color: FoldoutColor.Red)]
    public bool isReloading;

    public int currentAmmo;

    public float recoilOffset;
}
```

---

# Network Behaviour Support (Unity Netcode for GameObjects)

`NetworkEditorOverride.cs` extends the same `[Foldout]` workflow to `NetworkBehaviour`
(Unity Netcode for GameObjects), including proper handling of `NetworkVariable<T>` and
`NetworkList<T>` fields.

This file has a compile-time dependency on `Unity.Netcode.Runtime`, referenced by name in
`TyroByte.UIFoldout.Editor.asmdef`. That reference only resolves when
`com.unity.netcode.gameobjects` is installed, so projects without it are unaffected by the
asmdef itself. Compilation of the file is additionally **opt-in** behind a manual scripting
define, so projects with Netcode installed but not yet using it stay unaffected too:

```text
Project Settings → Player → Other Settings → Scripting Define Symbols
Add: TYROBYTE_NETCODE_GAMEOBJECTS
```

Only add this if `com.unity.netcode.gameobjects` is installed in the project.

```csharp
using Unity.Netcode;
using TyroByte;

public class PlayerNetwork : NetworkBehaviour
{
    [Foldout("Health", true)]
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);

    public NetworkVariable<bool> isAlive = new NetworkVariable<bool>(true);

    [Foldout("Inventory", true)]
    public NetworkList<int> itemIds;
}
```

### What it resolves

`NetworkVariableBase` (the base of `NetworkVariable<T>` and `NetworkList<T>`) is
`[Serializable]`, so it shows up in Unity's `SerializedObject` iteration — but drawing it
with a plain `PropertyField` renders its internal serialized layout, not the value, and
ignores Netcode's write-permission model entirely. This editor instead:

- Detects `NetworkVariableBase`-derived fields via reflection and pulls them out of the
  normal `SerializedProperty` pass, so they never render as broken nested fields.
- Draws `NetworkVariable<T>.Value` using a type-appropriate field (int/float/bool/string/
  enum/Vector2/3/4/Quaternion/Color), falling back to a read-only label for unsupported
  types (e.g. custom `INetworkSerializable` structs).
- Disables editing when the local client can't write to the variable (checked via
  `NetworkVariableBase.CanClientWrite`, matching `WritePerm` — `Server` vs `Owner` — and
  actual runtime ownership), with a small note explaining why it's locked, instead of
  silently discarding edits the network layer would reject.
- Shows `NetworkList<T>` read-only with an item count, matching Netcode's own inspector.
- Warns if the `GameObject` (or its parents) has no `NetworkObject`, since such a
  `NetworkBehaviour` won't function at runtime.
- Respects `[HideInInspector]` and `[NonSerialized]` on NetworkVariable fields.

### Known limitations

- No multi-object editing (`[CanEditMultipleObjects]` is intentionally not used) —
  NetworkVariable values are read/written against a single `target`.
- This editor takes over from Netcode's own `NetworkBehaviourEditor` for every
  `NetworkBehaviour` in the project; that's expected since a project-defined
  `[CustomEditor]` takes precedence over a package's built-in one.

---

# License

MIT License

Feel free to use, modify, and distribute in personal or commercial projects.
