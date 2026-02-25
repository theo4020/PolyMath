# Design : Dual-Algo Display, Transforms & Polygon Fix
Date: 2026-02-25

## Scope
Seven improvements across three areas: polygon right-click fix, dual-curve
Bézier display (Pascal + Casteljau simultaneously), pre-made 50-point demo
curve, and a full transform overhaul (configurable steps, H+V shear, pivot-
aware rotation).

---

## 1. Bug Fix — Right-click in Polygon mode

### Problem
When both polygons are closed and clipping is done, `_polyMgr.HandleRightClick()`
returns `false` and nothing happens — even though `StatusText` reads
*"Right-click menu → Reset to restart."*

### Fix
In `Main.HandleRightClick()`, if the polygon manager returns `false`
(nothing to close), fall through and show the popup menu.

```csharp
private void HandleRightClick()
{
    if (_mode == AppMode.Polygon)
    {
        if (!_polyMgr.HandleRightClick())   // false = clipping already done
        {
            _menu.Position = (Vector2I)GetViewport().GetMousePosition();
            _menu.Popup();
        }
        return;
    }
    _menu.Position = (Vector2I)GetViewport().GetMousePosition();
    _menu.Popup();
}
```

---

## 2. Dual-Curve Display (Pascal + Casteljau)

### Goal
Show the same control polygon evaluated by *both* algorithms at the same time,
in different colours, so students can verify they produce identical results.

### Design
**New BezierManager fields:**
```csharp
private bool _showPascal     = true;
private bool _showCasteljau  = false;
public Color PascalColor     { get; set; } = new Color(0.2f, 0.6f, 1f);    // blue
public Color CasteljauColor  { get; set; } = new Color(1f, 0.35f, 0.1f);   // orange-red
```

**DrawCurve changes:**
- Compute `GetPoints(false)` if `_showPascal`, draw in `PascalColor`
- Compute `GetPoints(true)` if `_showCasteljau`, draw in `CasteljauColor`
- Share the single `curvePts` reference for fill (use Pascal points when both active)

**New toggle methods:** `ToggleShowPascal()`, `ToggleShowCasteljau()`

**Keys (Bézier mode):** `P` toggles Pascal, `C` toggles Casteljau

**Sidebar buttons:** "Pascal [P]" and "Casteljau [C]"

**StatusText**: shows `[Pascal][Casteljau]` flags

---

## 3. Pre-made 50-point Demo Curve

### Goal
One button creates a new Bézier curve with 50 control points in a 3-cycle sine
wave, demonstrating the Runge oscillation effect of high-degree polynomials.

### Formula
```
for i in 0..49:
    x = 80 + i * 33
    y = 420 + 170 * sin(i * 6π / 49)
```
(centres in a 1920×1080 canvas, ~3 full cycles)

**Method:** `BezierManager.LoadDemoSine50()` — adds a new curve and makes it active.

---

## 4. Transform Overhaul

### 4a. Configurable Steps

Each manager (BezierManager, BSplineManager) stores four step fields:

| Field          | Default | Sidebar ±increment |
|----------------|---------|-------------------|
| `_transStep`   | 30 f    | ±5 px             |
| `_rotStep`     | π/12    | ±π/36 (5°)        |
| `_scaleStep`   | 1.10 f  | ±0.05             |
| `_shearStep`   | 0.10 f  | ±0.05             |

Sidebar for each: `[−] value [+]` label line.

Public step-adjust methods: `TransStepUp/Down`, `RotStepUp/Down`,
`ScaleStepUp/Down`, `ShearStepUp/Down`.

### 4b. Pivot-Aware Rotation & Scale

**`GetPivot() : Point2D`** (BezierManager and BSplineManager)
- If `_dragIndex >= 0` and a control point exists at that index → use it
- Otherwise → centroid of all control points of the active curve

**Visual:** small white circle drawn at the pivot point in edit mode.

**Rotation matrix** (around pivot p):
```
M = Translation(p.x, p.y) × Rotation(θ) × Translation(-p.x, -p.y)
```

**Scale matrix** (around pivot p):
```
M = Translation(p.x, p.y) × Scaling(s, s) × Translation(-p.x, -p.y)
```

**New manager methods** (replace raw `ApplyTransform` calls from Main):
```csharp
public void Translate(float dx, float dy)
public void Rotate(float sign)      // sign = +1 CW, −1 CCW
public void Scale(float factor)     // factor > 1 = grow
public void ShearH(float sign)
public void ShearV(float sign)
```

### 4c. Keyboard Map (Bézier + BSpline modes)

| Key             | Action                                  |
|-----------------|-----------------------------------------|
| `←→↑↓`         | Translate ±`_transStep`                 |
| `Shift+←→↑↓`   | Translate ±`_transStep/10` (fine)      |
| `R`             | Rotate CW around pivot                  |
| `Shift+R`       | Rotate CCW around pivot                 |
| `S`             | Scale up around pivot                   |
| `Shift+S`       | Scale down around pivot                 |
| `H` / `Shift+H` | Shear Hx + / −                          |
| `V` / `Shift+V` | Shear Hy + / − *(new)*                 |

### 4d. Sidebar Transform Section

```
── TRANSFORMÉES ──
[←] [→] [↑] [↓]        (translation)
[↻ CW]  [↺ CCW]        (rotation)
[⊕ Scale+] [⊖ Scale-]
[Cis.H+] [Cis.H-]
[Cis.V+] [Cis.V-]      (new)

Pas Trans.:  [−] 30px [+]
Pas Rot.:    [−]  15° [+]
Pas Scale:   [−] 10%  [+]
Pas Cis.:    [−] 0.10 [+]
```

---

## Files Changed

| File | Changes |
|------|---------|
| `Main.cs` | Fix `HandleRightClick`, new keys P/C/V, new sidebar buttons+step labels, call manager methods instead of raw `ApplyTransform` |
| `BezierManager.cs` | `_showPascal/Casteljau`, `LoadDemoSine50`, step fields, `GetPivot`, `Rotate/Scale/ShearH/ShearV/Translate`, step-adjust methods, pivot indicator draw |
| `BSplineManager.cs` | Same step/pivot additions as BezierManager |
