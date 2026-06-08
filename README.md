# Maths Power — Godot 4.5 / C# (.NET)

Visualisation interactive des deux parties du projet :

- **Partie 1 — Primitives d'extrusion** : sphère, cylindre, cône, extrusion
  simple, révolution, extrusion généralisée. Profil 2D éditable (Bézier).
- **Partie 2 — Surfaces de Bézier** : produit tensoriel, trois algorithmes
  d'évaluation (directe, double De Casteljau ×2), subdivision en 4.

## Stack

- Godot **4.5** (.NET), renderer Forward+
- C# / **.NET 8** (SDK 9 installé)
- `Godot.NET.Sdk/4.5.0`

> Le dossier `rust/` est l'ancienne implémentation godot-rust, conservée pour
> référence. Elle n'est plus chargée (le `.gdextension` est archivé dans
> `_rust_archive/`).

## Build & Run

```powershell
dotnet build "Maths Power.csproj"
```

Puis ouvrir le projet dans l'éditeur Godot 4.5 .NET et appuyer sur **F5**
(ou utiliser le bouton *Build* puis *Play* de l'éditeur).

## Architecture (src/)

### Couche math (pure, convention z up = polycopié)
| Fichier | Rôle |
|---|---|
| `Bezier.cs` | De Casteljau 2D + échantillonnage de courbe |
| `Surfaces.cs` | 6 primitives du Cours 1 |
| `BezierSurface.cs` | `ControlNet` + 3 algos + subdivision en 4 |
| `SurfaceGrid.cs` | grille `(m+1)×(p+1)`, positions + normales (diffs finies) |
| `MeshBuilder.cs` | `SurfaceGrid → ArrayMesh`, viridis, tubes, wireframe |

### Nœuds Godot
| Fichier | Type de base | Rôle |
|---|---|---|
| `SurfaceCours1.cs` | MeshInstance3D | primitive paramétrée + overlay courbe source |
| `SurfaceCours2.cs` | MeshInstance3D | surface Bézier + polyèdre + subdivision |
| `GroundGrid.cs` | MeshInstance3D | grille de sol + axes monde |
| `OrbitCamera.cs` | Camera3D | orbite (drag), pan (clic-milieu), zoom (molette) |
| `ProfileEditor.cs` | Control | éditeur 2D de courbe (profil / âme) |
| `UiPanel.cs` | Control | panneau de la Partie 1 |
| `UiPanelCours2.cs` | Control | panneau de la Partie 2 |
| `TopBar.cs` | Control | onglets + bascule de thème |
| `ThemeState.cs` / `UiTheme.cs` | — | palette sombre/clair + helpers de stylage |

### Conventions d'axes
- Math : **z vertical** (polycopié).
- Rendu Godot : **y vertical**.
- Mapping `(x, y, z) → (x, z, -y)` appliqué une seule fois dans `MeshBuilder`
  (positions **et** normales).

## État vs énoncé

**Partie A** : Bézier ✅, maillage triangulaire ✅, normales ✅, coloriage ✅,
lumière ✅, extrusion interactive ✅. À faire : B-splines, NURBS, sélection de
courbe, texturage.

**Partie B** : réseau de contrôle 3D ✅, produit tensoriel direct ✅, double
De Casteljau ✅ (×2), subdivision en 4 ✅, maillage + normales + coloriage ✅.
À faire : raccord de patches (bonus), texturage.
