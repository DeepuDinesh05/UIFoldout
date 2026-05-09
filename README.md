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

1. Download or clone the repository
2. Copy the `TyroByte` folder into your Unity project's `Assets` folder
3. Let Unity recompile

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

# Combined Example

```csharp
using UnityEngine;
using UnityEngine.Events;
using TyroByte;

public class BlinkEffect : MonoBehaviour
{
    [Foldout(
        "Blink Settings",
        true,
        startExpanded: true,
        color: FoldoutColor.Teal)]
    [Range(0f, 1f)]
    public float smoothness = 1f;

    [Range(0f, 1f)]
    public float curvature = 1f;

    [Foldout(
        "Events",
        true,
        color: FoldoutColor.Green)]
    public UnityEvent OnBlinkIn;

    public UnityEvent OnBlinkComplete;

    [Foldout(
        "Debug",
        true,
        readOnly: true,
        color: FoldoutColor.Red)]
    public Material curvedMaterial;

    public float time;
}
```
---

# License

MIT License

Feel free to use, modify, and distribute in personal or commercial projects.
