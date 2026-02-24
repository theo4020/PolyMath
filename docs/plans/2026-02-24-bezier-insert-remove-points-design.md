# Design — Insertion / suppression de points de contrôle Bézier

**Date :** 2026-02-24

## Contexte

En mode Édition, l'utilisateur peut sélectionner et déplacer les points de contrôle
existants. Il n'existe aucun moyen d'insérer un point entre deux points existants ni
de supprimer un point par simple clic.

## Décisions retenues

| Action | Geste | Condition |
|--------|-------|-----------|
| Insérer un point | Clic gauche sur l'arête P[i]→P[i+1] (rayon 12 px) | Mode Édition uniquement |
| Supprimer un point | Double-clic sur un point existant | Mode Édition uniquement |
| Supprimer le point sélectionné | Touche Suppr (existant) | Mode Édition, point sélectionné |

## Changements de code

### BezierCurve.cs
- `InsertPoint(int index, Point2D p)` — insère un point à l'index donné et invalide le cache Pascal.

### BezierManager.cs
- `DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b) → float` — distance d'un point
  à un segment (projection clampée).
- `HandleLeftClick` (mode Édition) — après échec de sélection d'un point existant,
  cherche l'arête la plus proche ; si distance ≤ SELECT_THRESHOLD, appelle
  `InsertPoint(i+1, ...)` et sélectionne le nouveau point (`_dragIndex = i+1`, `_dragging = true`).
- `HandleDoubleClick(Vector2 mouse)` — itère les points ; si un point est dans le rayon,
  appelle `RemovePoint(i)` et réinitialise `_dragIndex`/`_dragging`.
- `StatusText` — mise à jour du texte d'aide en mode Édition.

### Main.cs
- `_UnhandledInput` — détecte `InputEventMouseButton` avec `DoubleClick = true` sur
  `MouseButton.Left` → `_bezMgr.HandleDoubleClick(mouse)`.

## Non-inclus (YAGNI)
- Insertion par clic sur la courbe évaluée (plus complexe, pas demandé).
- Mode séparé "Insertion" / "Suppression" (ajout en mode Édition suffit).
