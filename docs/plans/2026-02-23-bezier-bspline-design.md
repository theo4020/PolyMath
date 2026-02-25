# Design: Bézier Curves, BSplines & NURBS

**Date:** 2026-02-23
**Deadlines:** Projet 2 (Bézier) — Wed 25/02/2026 | Projet 3 (BSplines/NURBS) — Fri 27/02/2026
**Engine:** Godot 4 / C#
**Course:** M1 – T2 – Maths Infographie Avancées 2 — BIANCHINI Marc

---

## 1. Context

The project builds cumulatively on T1 (Fenêtrage & Remplissage), which is already implemented:
- `SutherlandHodgman.cs` — polygon clipping
- `LCAFill.cs` — scan-line fill
- `Polygon.cs` — polygon data structure
- `Point2D.cs` — 2D point with operators
- `Matrix3x3.cs` — 3×3 homogeneous transforms (Translation, Rotation, Scaling)
- `Main.cs` — Godot scene (mouse input, draw, state machine)

---

## 2. Architecture

```
poly-maths/
├── Main.cs                        ← refactored: mode dispatcher + menu
├── Algorithms/
│   ├── Point2D.cs                 ← unchanged
│   ├── Polygon.cs                 ← unchanged
│   ├── Matrix3x3.cs               ← add Shearing(shx, shy)
│   ├── SutherlandHodgman.cs       ← unchanged
│   ├── LCAFill.cs                 ← unchanged
│   ├── BezierCurve.cs             ← NEW
│   ├── BSplineCurve.cs            ← NEW
│   └── NURBSCurve.cs              ← NEW
├── Managers/
│   ├── PolygonManager.cs          ← NEW (T1 logic extracted from Main.cs)
│   ├── BezierManager.cs           ← NEW
│   └── BSplineManager.cs          ← NEW
└── Tests/
    ├── PolygonTestSuite.cs        ← unchanged
    ├── BezierTestSuite.cs         ← NEW
    └── BSplineTestSuite.cs        ← NEW
```

### Mode Enum (Main.cs)
```csharp
enum AppMode { Polygon, Bezier, BSpline }
```

### Menu System
- Godot `PopupMenu` on a `CanvasLayer` (right-click to open)
- Context-sensitive items per mode
- HUD label in top-left shows current mode + step + active curve index

---

## 3. Bézier Module (Projet 2)

### 3.1 `Algorithms/BezierCurve.cs`

Pure math class, no Godot dependencies.

**Properties:**
- `List<Point2D> ControlPoints` — the n+1 control points
- `int Step` — number of curve segments (default: 100)

**Methods:**
- `EvaluateDirect(float t)` — Bernstein formula using Pascal's triangle; coefficients precomputed once on control point change
- `EvaluateCasteljau(float t)` — iterative de Casteljau (course version)
- `GetPoints(bool useCasteljau)` — samples curve at `Step+1` values of t ∈ [0,1], returns `List<Point2D>`
- `BenchmarkBoth()` — returns `(long msDirect, long msCasteljau)` using `Stopwatch`

**Pascal's triangle:** computed once per n via `C[i][j] = C[i-1][j-1] + C[i-1][j]`.

**De Casteljau (iterative):**
```
for r = 1..n:
  for i = 0..n-r:
    d[i] = (1-t)*d[i] + t*d[i+1]
return d[0]
```

### 3.2 `Managers/BezierManager.cs`

**State:**
- `LinkedList<BezierCurve> Curves`
- `LinkedListNode<BezierCurve> ActiveNode` — currently selected curve
- `int? SelectedVertexIndex` — for drag/delete
- `bool UseCasteljau` — toggle method
- `bool EditMode` — append vs. edit

**Input handling (delegated from Main.cs):**
- Left-click (AppendMode): add control point to active curve; draw control polygon in real time
- Left-click (EditMode): select nearest control point within threshold
- Left-drag (EditMode): move selected control point → redraw curve
- Right-click: open context menu
- `+` / `-` keys: increase/decrease Step
- `Delete` key: remove selected control point or active curve

**Geometric transforms:**
- Apply `Matrix3x3` (Translation / Rotation / Scaling / Shearing) to all control points of active curve
- Real-time redraw after each transform step

**Multiplicity:** duplicate control points allowed; no special treatment needed — curve naturally attracted

**C0/C1/C2 joining:**
- C0: last point of curve A == first point of curve B
- C1: additionally `B.P[1] = 2*A.P[n] - A.P[n-1]`
- C2: additionally adjusts `B.P[2]` based on second derivative continuity

**Clipping & filling closed curves:**
- Convert curve points to `Polygon` → pass to `SutherlandHodgman` and `LCAFill`

**Drawing (called by Main._Draw):**
- Control polygon: dashed lines between control points
- Curve: segments connecting consecutive evaluated points
- Selected point: highlighted circle
- Benchmark result: shown as HUD text

### 3.3 Menu items (BEZIER mode)
```
New curve | Delete active curve | [separator]
Method: Direct ✓ | Method: Casteljau | [separator]
Step + | Step - | [separator]
Edit mode | Append mode | [separator]
Duplicate point (multiplicity) | [separator]
Join C0 | Join C1 | Join C2 | [separator]
Transform: Translate | Rotate | Scale | Shear | [separator]
Close curve | Fill curve | [separator]
Benchmark | Reset all
```

---

## 4. BSpline & NURBS Module (Projet 3)

### 4.1 `Algorithms/BSplineCurve.cs`

Pure math class.

**Properties:**
- `List<Point2D> ControlPoints`
- `int Degree` (p, default 3)
- `List<float> KnotVector`
- `bool IsClamped`
- `int Step` (default 100)

**Methods:**
- `SetUniformKnots()` — uniform open knot vector
- `SetClampedKnots()` — clamped (repeated endpoints): `p+1` zeros, interior uniform, `p+1` ones
- `BasisFunction(int i, int p, float t)` — Cox-de Boor, guards against `0/0` (returns 0)
- `Evaluate(float t)` — `Σ Ni,p(t) * Pi`
- `GetPoints()` — samples over `[knot[p], knot[n+1]]`

**Cox-de Boor recursion:**
```
N(i,0,t) = 1 if knot[i] <= t < knot[i+1], else 0
N(i,p,t) = (t-knot[i])/(knot[i+p]-knot[i]) * N(i,p-1,t)
           + (knot[i+p+1]-t)/(knot[i+p+1]-knot[i+1]) * N(i+1,p-1,t)
guard: 0/0 = 0
```

### 4.2 `Algorithms/NURBSCurve.cs`

Extends `BSplineCurve`.

**Additional property:** `List<float> Weights` (default all 1.0)

**Override `Evaluate(float t)`:**
```
numerator   = Σ Ni,p(t) * wi * Pi
denominator = Σ Ni,p(t) * wi
return numerator / denominator
```

**Factory methods (static):**
- `Circle(Point2D center, float radius)` — 9 control points, specific weights (√2/2 for mid-points)
- `Ellipse(Point2D center, float a, float b)`
- `Parabola(Point2D vertex, float scale)`
- `Hyperbola(Point2D center, float a, float b)`

### 4.3 `Managers/BSplineManager.cs`

Same structure as `BezierManager`:
- `LinkedList<BSplineCurve> Curves`
- `LinkedList<NURBSCurve> NurbsCurves`
- Degree ± via menu
- Knot type toggle
- Weight editing (select point → scroll wheel changes weight)
- Same drag/delete/transform logic

### 4.4 Menu items (BSPLINE mode)
```
New BSpline | New NURBS | Delete active | [separator]
Degree + | Degree - | [separator]
Knots: Uniform ✓ | Knots: Clamped | [separator]
Edit weights | [separator]
Transform: Translate | Rotate | Scale | Shear | [separator]
Demo: Circle | Ellipse | Parabola | Hyperbola | [separator]
Reset all
```

---

## 5. Matrix3x3 Addition

Add to `Algorithms/Matrix3x3.cs`:
```csharp
public static Matrix3x3 Shearing(float shx, float shy)
{
    return new Matrix3x3(new float[,]
    {
        { 1, shx, 0 },
        { shy, 1, 0 },
        { 0,   0, 1 }
    });
}
```

---

## 6. Test Suites

### `BezierTestSuite.cs`
- Degree-1 Bézier = straight line (P0→P1)
- Degree-2 passes through midpoint at t=0.5
- Direct vs Casteljau produce identical results (within 1e-4)
- Step change affects point count
- C0/C1/C2 joining: verify endpoint/tangent/curvature conditions
- Benchmark: Casteljau ≤ Direct for n > 50 (verify both run without crash)

### `BSplineTestSuite.cs`
- Uniform BSpline: curve does not pass through endpoints
- Clamped BSpline: curve passes through first and last control points
- Degree 1 = polyline through all control points
- NURBS with all weights=1 equals BSpline
- NURBS circle: verify 4 cardinal points lie on circle
- Cox-de Boor 0/0 guard: no exceptions on degenerate knot vectors

---

## 7. Implementation Order

1. `Matrix3x3.cs` — add Shearing (5 min)
2. `Algorithms/BezierCurve.cs` — math only (30 min)
3. `Managers/PolygonManager.cs` — extract T1 from Main.cs (20 min)
4. `Managers/BezierManager.cs` — state + input (45 min)
5. `Main.cs` — refactor to mode dispatcher + PopupMenu (30 min)
6. `Tests/BezierTestSuite.cs` (20 min)
7. `Algorithms/BSplineCurve.cs` (30 min)
8. `Algorithms/NURBSCurve.cs` (20 min)
9. `Managers/BSplineManager.cs` (40 min)
10. `Tests/BSplineTestSuite.cs` (20 min)

**Total estimated:** ~4h — well within the 2-day window for Bézier and 4-day window for BSplines.
