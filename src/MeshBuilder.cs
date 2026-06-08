using Godot;

namespace MathsPower;

// Conversion SurfaceGrid → ArrayMesh + palette viridis.
// Convention d'axes : la couche math produit z vertical (polycopié). Le
// mapping z_math → y_godot se fait ici : (x, y, z) → (x, z, -y).
public static class MeshBuilder
{
    private static readonly (float t, byte r, byte g, byte b)[] ViridisStops =
    {
        (0.0f, 68, 1, 84),
        (0.25f, 59, 82, 139),
        (0.5f, 33, 145, 140),
        (0.75f, 94, 201, 98),
        (1.0f, 253, 231, 37),
    };

    public static Color Viridis(float t)
    {
        t = Mathf.Clamp(t, 0.0f, 1.0f);
        for (int s = 0; s < ViridisStops.Length - 1; s++)
        {
            var a = ViridisStops[s];
            var b = ViridisStops[s + 1];
            if (t <= b.t)
            {
                float f = (t - a.t) / Mathf.Max(b.t - a.t, 1e-9f);
                return new Color(
                    (a.r * (1 - f) + b.r * f) / 255f,
                    (a.g * (1 - f) + b.g * f) / 255f,
                    (a.b * (1 - f) + b.b * f) / 255f);
            }
        }
        var last = ViridisStops[^1];
        return new Color(last.r / 255f, last.g / 255f, last.b / 255f);
    }

    // Mapping polycopié (z up) → Godot (y up).
    private static Vector3 ToGodot(Vector3 v) => new(v.X, v.Z, -v.Y);

    // `fillMaterial` : matériau de la surface (typiquement le ShaderMaterial
    // procédural). Si null, repli sur un StandardMaterial3D à couleurs vertex.
    // Le mesh émet UV (paramétrage u,v) + UV2.x (hauteur normalisée) pour le
    // shader, et COLOR (viridis) pour le repli.
    public static ArrayMesh SurfaceGridToMesh(SurfaceGrid grid, bool showWireframe, Material? fillMaterial = null)
    {
        int np = grid.P + 1;
        int nVerts = (grid.M + 1) * np;
        var (zmin, zmax) = grid.ZRange();
        float zSpan = Mathf.Max(zmax - zmin, 1e-9f);
        float invM = grid.M > 0 ? 1.0f / grid.M : 0.0f;
        float invP = grid.P > 0 ? 1.0f / grid.P : 0.0f;

        var verts = new Vector3[nVerts];
        var colors = new Color[nVerts];
        var normals = new Vector3[nVerts];
        var uvs = new Vector2[nVerts];
        var uv2s = new Vector2[nVerts];
        for (int i = 0; i <= grid.M; i++)
        {
            for (int j = 0; j <= grid.P; j++)
            {
                int idx = i * np + j;
                Vector3 v = grid.Data[idx];
                Vector3 n = grid.Normals[idx];
                float t = (v.Z - zmin) / zSpan;
                verts[idx] = ToGodot(v);
                normals[idx] = ToGodot(n);
                colors[idx] = Viridis(t);
                uvs[idx] = new Vector2(i * invM, j * invP);
                uv2s[idx] = new Vector2(t, 0.0f);
            }
        }

        var indices = new int[grid.M * grid.P * 6];
        int k = 0;
        for (int i = 0; i < grid.M; i++)
        {
            for (int j = 0; j < grid.P; j++)
            {
                int v00 = i * np + j;
                int v10 = (i + 1) * np + j;
                int v11 = (i + 1) * np + j + 1;
                int v01 = i * np + j + 1;
                indices[k] = v00; indices[k + 1] = v10; indices[k + 2] = v11;
                indices[k + 3] = v00; indices[k + 4] = v11; indices[k + 5] = v01;
                k += 6;
            }
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.TexUV2] = uv2s;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        Material fillMat = fillMaterial ?? new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            VertexColorIsSrgb = true,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
        mesh.SurfaceSetMaterial(0, fillMat);

        if (showWireframe)
        {
            var wireArrays = BuildWireframeArrays(grid);
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, wireArrays);
            var wireMat = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                NoDepthTest = true,
                AlbedoColor = Palette.Current().Wireframe,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            };
            mesh.SurfaceSetMaterial(1, wireMat);
        }

        return mesh;
    }

    // Arêtes dédupliquées : chaque arête interne une seule fois.
    private static Godot.Collections.Array BuildWireframeArrays(SurfaceGrid grid)
    {
        int np = grid.P + 1;
        int nLines = (grid.M + 1) * grid.P + grid.M * np;
        var verts = new Vector3[nLines * 2];
        int k = 0;
        for (int i = 0; i <= grid.M; i++)
            for (int j = 0; j < grid.P; j++)
            {
                verts[k++] = ToGodot(grid.Data[i * np + j]);
                verts[k++] = ToGodot(grid.Data[i * np + j + 1]);
            }
        for (int j = 0; j < np; j++)
            for (int i = 0; i < grid.M; i++)
            {
                verts[k++] = ToGodot(grid.Data[i * np + j]);
                verts[k++] = ToGodot(grid.Data[(i + 1) * np + j]);
            }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        return arrays;
    }

    // Tube triangulé le long d'une polyligne (déjà en convention Godot).
    // Visible par-dessus le wireframe (unshaded + NoDepthTest).
    public static ArrayMesh BuildTubeMesh(Vector3[] points, float radius, Color color, int nSides)
    {
        var mesh = new ArrayMesh();
        AppendTube(mesh, points, radius, color, nSides);
        return mesh;
    }

    // Ajoute un tube comme surface supplémentaire à un mesh existant
    // (permet d'empiler plusieurs courbes dans un seul MeshInstance3D).
    public static void AppendTube(ArrayMesh mesh, Vector3[] points, float radius, Color color, int nSides)
    {
        if (points.Length < 2 || nSides < 3)
            return;

        int nPoints = points.Length;
        var verts = new Vector3[nPoints * nSides];
        var colors = new Color[nPoints * nSides];

        for (int p = 0; p < nPoints; p++)
        {
            Vector3 tangentRaw = p == 0 ? points[1] - points[0]
                : p == nPoints - 1 ? points[nPoints - 1] - points[nPoints - 2]
                : points[p + 1] - points[p - 1];
            float tlen = tangentRaw.Length();
            Vector3 tangent = tlen > 1e-9f ? tangentRaw / tlen : new Vector3(0, 1, 0);

            Vector3 worldUp = new(0, 1, 0);
            Vector3 auxCross = tangent.Cross(worldUp);
            Vector3 right = auxCross.Length() > 0.1f
                ? auxCross.Normalized()
                : tangent.Cross(new Vector3(1, 0, 0)).Normalized();
            Vector3 up = right.Cross(tangent);

            for (int s = 0; s < nSides; s++)
            {
                float angle = Mathf.Tau * s / nSides;
                Vector3 offset = right * (Mathf.Cos(angle) * radius) + up * (Mathf.Sin(angle) * radius);
                int idx = p * nSides + s;
                verts[idx] = points[p] + offset;
                colors[idx] = color;
            }
        }

        var indices = new int[(nPoints - 1) * nSides * 6];
        int ki = 0;
        for (int p = 0; p < nPoints - 1; p++)
            for (int s = 0; s < nSides; s++)
            {
                int sNext = (s + 1) % nSides;
                int v00 = p * nSides + s;
                int v01 = p * nSides + sNext;
                int v10 = (p + 1) * nSides + s;
                int v11 = (p + 1) * nSides + sNext;
                indices[ki] = v00; indices[ki + 1] = v10; indices[ki + 2] = v11;
                indices[ki + 3] = v00; indices[ki + 4] = v11; indices[ki + 5] = v01;
                ki += 6;
            }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        int surfaceIdx = mesh.GetSurfaceCount() - 1;
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true,
            NoDepthTest = true,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            AlbedoColor = color,
        };
        mesh.SurfaceSetMaterial(surfaceIdx, material);
    }

    // Petit marqueur sphérique unshaded, visible par-dessus tout.
    public static Mesh MarkerSphere(float radius, Color color)
    {
        var sphere = new SphereMesh { Radius = radius, Height = radius * 2.0f, RadialSegments = 14, Rings = 8 };
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            NoDepthTest = true,
            AlbedoColor = color,
        };
        sphere.Material = mat;
        return sphere;
    }
}
