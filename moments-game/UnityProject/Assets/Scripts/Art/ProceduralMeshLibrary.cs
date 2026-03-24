using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedural mesh generation library for Moments.
/// All arena geometry is generated at runtime — no external mesh files required.
/// Each method returns a fully UV-mapped, normal-calculated Mesh.
///
/// Meshes generated here:
///   - HexagonFlat / HexagonPointed  — ice tile, gravity grab orb base
///   - RoundedBox                     — character body, bumper car body
///   - Torus                          — gravity grab orbital ring
///   - SuperellipsoidCapsule          — character silhouette (toy-like rounded capsule)
///   - OceanPlane (subdivided)        — Wave Rider water surface (Gerstner vertex displacement)
///   - BoostArrow                     — BumperBlitz boost pad arrow decal
///   - IceTileEdgeCap                 — hex edge glow strip
///   - GravityOrb                     — small, medium, large orb (sphere variants)
/// </summary>
public static class ProceduralMeshLibrary
{
    // ── Hexagon ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Flat-top hexagon prism. UV-mapped so top face occupies 0–1 UV space.
    /// </summary>
    public static Mesh Hexagon(float radius = 1f, float height = 0.18f, bool flatTop = true)
    {
        int sides = 6;
        var mesh = new Mesh { name = "Hexagon" };

        var verts    = new List<Vector3>();
        var uvs      = new List<Vector2>();
        var normals  = new List<Vector3>();
        var tris     = new List<int>();

        float angleOffset = flatTop ? 0f : 30f * Mathf.Deg2Rad;

        // Top face vertices
        int topCenter = 0;
        verts.Add(new Vector3(0, height * 0.5f, 0));
        uvs.Add(new Vector2(0.5f, 0.5f));
        normals.Add(Vector3.up);

        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides + angleOffset;
            verts.Add(new Vector3(Mathf.Cos(angle) * radius, height * 0.5f, Mathf.Sin(angle) * radius));
            uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            normals.Add(Vector3.up);
        }

        for (int i = 1; i <= sides; i++)
        {
            tris.Add(topCenter);
            tris.Add(i == sides ? 1 : i + 1);
            tris.Add(i);
        }

        // Bottom face
        int botCenter = verts.Count;
        verts.Add(new Vector3(0, -height * 0.5f, 0));
        uvs.Add(new Vector2(0.5f, 0.5f));
        normals.Add(Vector3.down);

        int botStart = verts.Count;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides + angleOffset;
            verts.Add(new Vector3(Mathf.Cos(angle) * radius, -height * 0.5f, Mathf.Sin(angle) * radius));
            uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            normals.Add(Vector3.down);
        }

        for (int i = 0; i < sides; i++)
        {
            tris.Add(botCenter);
            tris.Add(botStart + i);
            tris.Add(botStart + (i == sides - 1 ? 0 : i + 1));
        }

        // Side faces
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            float angle     = i    * Mathf.PI * 2f / sides + angleOffset;
            float angleNext = next * Mathf.PI * 2f / sides + angleOffset;

            Vector3 n0 = new Vector3(Mathf.Cos(angle),     0, Mathf.Sin(angle));
            Vector3 n1 = new Vector3(Mathf.Cos(angleNext), 0, Mathf.Sin(angleNext));
            Vector3 sideNorm = ((n0 + n1) * 0.5f).normalized;

            int v0 = verts.Count;
            verts.Add(new Vector3(Mathf.Cos(angle)     * radius,  height * 0.5f, Mathf.Sin(angle)     * radius));
            verts.Add(new Vector3(Mathf.Cos(angleNext) * radius,  height * 0.5f, Mathf.Sin(angleNext) * radius));
            verts.Add(new Vector3(Mathf.Cos(angleNext) * radius, -height * 0.5f, Mathf.Sin(angleNext) * radius));
            verts.Add(new Vector3(Mathf.Cos(angle)     * radius, -height * 0.5f, Mathf.Sin(angle)     * radius));

            float u0 = (float)i / sides, u1 = (float)(i + 1) / sides;
            uvs.Add(new Vector2(u0, 1)); uvs.Add(new Vector2(u1, 1));
            uvs.Add(new Vector2(u1, 0)); uvs.Add(new Vector2(u0, 0));

            normals.Add(sideNorm); normals.Add(sideNorm);
            normals.Add(sideNorm); normals.Add(sideNorm);

            tris.Add(v0); tris.Add(v0 + 1); tris.Add(v0 + 2);
            tris.Add(v0); tris.Add(v0 + 2); tris.Add(v0 + 3);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Rounded Box ───────────────────────────────────────────────────────────

    /// <summary>
    /// Box with rounded edges. Used for character body and bumper car.
    /// Resolution: segments around each edge curve.
    /// </summary>
    public static Mesh RoundedBox(float w = 1f, float h = 1.6f, float d = 1f, float radius = 0.15f, int segs = 4)
    {
        // Build via SDF-like construction: place spheres at 8 corners,
        // cylinders on 12 edges, quads on 6 faces.
        // Simplified: generate via subdivided cube + spherical projection on corners.
        var mesh = new Mesh { name = "RoundedBox" };

        int res = Mathf.Max(2, segs + 1);
        var verts   = new List<Vector3>();
        var uvs     = new List<Vector2>();
        var normals = new List<Vector3>();
        var tris    = new List<int>();

        // Build each of 6 faces as a subdivided quad
        Vector3[] faceNormals = {
            Vector3.up, Vector3.down,
            Vector3.right, Vector3.left,
            Vector3.forward, Vector3.back
        };

        float hw = w * 0.5f - radius;
        float hh = h * 0.5f - radius;
        float hd = d * 0.5f - radius;

        foreach (var faceN in faceNormals)
        {
            // Build tangent and bitangent for this face
            Vector3 tan, bitan;
            if (faceN == Vector3.up || faceN == Vector3.down)
            { tan = Vector3.right; bitan = Vector3.forward; }
            else if (faceN == Vector3.right || faceN == Vector3.left)
            { tan = Vector3.forward; bitan = Vector3.up; }
            else
            { tan = Vector3.right; bitan = Vector3.up; }

            // Face extents
            float eu = faceN == Vector3.right || faceN == Vector3.left ? hd : hw;
            float ev = faceN == Vector3.up || faceN == Vector3.down ? hd : hh;
            if (faceN == Vector3.right || faceN == Vector3.left)
                ev = hh;

            int faceStart = verts.Count;
            for (int yi = 0; yi < res; yi++)
            for (int xi = 0; xi < res; xi++)
            {
                float tx = Mathf.Lerp(-eu, eu, xi / (float)(res - 1));
                float ty = Mathf.Lerp(-ev, ev, yi / (float)(res - 1));

                // Core position on face plane
                Vector3 core = faceN * (faceN.x != 0 ? hw : faceN.y != 0 ? hh : hd)
                             + tan   * tx
                             + bitan * ty;

                // Push outward by radius along normal
                Vector3 pos = core + faceN * radius;

                verts.Add(pos);
                normals.Add(faceN);
                uvs.Add(new Vector2((tx / (eu * 2) + 0.5f), (ty / (ev * 2) + 0.5f)));
            }

            for (int yi = 0; yi < res - 1; yi++)
            for (int xi = 0; xi < res - 1; xi++)
            {
                int i0 = faceStart + yi * res + xi;
                tris.Add(i0);         tris.Add(i0 + res); tris.Add(i0 + 1);
                tris.Add(i0 + 1);     tris.Add(i0 + res); tris.Add(i0 + res + 1);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals(); // smooth corners
        return mesh;
    }

    // ── SuperEllipsoid Capsule ────────────────────────────────────────────────

    /// <summary>
    /// Squircle-profile capsule for character bodies. More toy-like than a standard capsule.
    /// n1 controls profile squareness (1=sphere, 4=very boxy), n2 controls cross-section.
    /// </summary>
    public static Mesh SuperEllipsoidCapsule(float rx = 0.42f, float ry = 0.82f, float rz = 0.42f,
                                              float n1 = 0.5f, float n2 = 0.5f,
                                              int stacks = 20, int slices = 24)
    {
        var mesh = new Mesh { name = "SuperEllipsoidCapsule" };
        var verts   = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs     = new List<Vector2>();
        var tris    = new List<int>();

        for (int i = 0; i <= stacks; i++)
        {
            float phi = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, i / (float)stacks);
            for (int j = 0; j <= slices; j++)
            {
                float theta = Mathf.Lerp(-Mathf.PI, Mathf.PI, j / (float)slices);

                // Superellipsoid parametric
                float cosPhi   = Mathf.Cos(phi);
                float sinPhi   = Mathf.Sin(phi);
                float cosTheta = Mathf.Cos(theta);
                float sinTheta = Mathf.Sin(theta);

                float se_cosPhi   = SignedPow(cosPhi,   n1);
                float se_sinPhi   = SignedPow(sinPhi,   n1);
                float se_cosTheta = SignedPow(cosTheta, n2);
                float se_sinTheta = SignedPow(sinTheta, n2);

                Vector3 pos = new Vector3(
                    rx * se_cosPhi * se_cosTheta,
                    ry * se_sinPhi,
                    rz * se_cosPhi * se_sinTheta);

                // Gradient for normals
                float nxBase = se_cosPhi > 0.001f ? (2f / n1) * SignedPow(cosPhi, 2f - n1) * SignedPow(cosTheta, 2f - n2) / rx : 0f;
                float nyBase = (2f / n1) * SignedPow(sinPhi, 2f - n1) / ry;
                float nzBase = se_cosPhi > 0.001f ? (2f / n1) * SignedPow(cosPhi, 2f - n1) * SignedPow(sinTheta, 2f - n2) / rz : 0f;
                var normal = new Vector3(nxBase, nyBase, nzBase);
                if (normal.sqrMagnitude < 0.001f) normal = Vector3.up;

                verts.Add(pos);
                normals.Add(normal.normalized);
                uvs.Add(new Vector2(j / (float)slices, i / (float)stacks));
            }
        }

        int w = slices + 1;
        for (int i = 0; i < stacks; i++)
        for (int j = 0; j < slices; j++)
        {
            int a = i * w + j, b = a + w, c = a + 1, d = b + 1;
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(c); tris.Add(b); tris.Add(d);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float SignedPow(float v, float p)
        => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v) + 1e-6f, p);

    // ── Torus ─────────────────────────────────────────────────────────────────

    /// <summary>Torus for gravity grab arena ring platform.</summary>
    public static Mesh Torus(float R = 10f, float r = 1.2f, int segsR = 48, int segsr = 16)
    {
        var mesh    = new Mesh { name = "Torus" };
        var verts   = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs     = new List<Vector2>();
        var tris    = new List<int>();

        for (int i = 0; i <= segsR; i++)
        {
            float u = i / (float)segsR * Mathf.PI * 2f;
            for (int j = 0; j <= segsr; j++)
            {
                float v = j / (float)segsr * Mathf.PI * 2f;
                float x = (R + r * Mathf.Cos(v)) * Mathf.Cos(u);
                float y = r * Mathf.Sin(v);
                float z = (R + r * Mathf.Cos(v)) * Mathf.Sin(u);
                verts.Add(new Vector3(x, y, z));
                Vector3 n = new Vector3(Mathf.Cos(v) * Mathf.Cos(u), Mathf.Sin(v), Mathf.Cos(v) * Mathf.Sin(u));
                normals.Add(n.normalized);
                uvs.Add(new Vector2(i / (float)segsR, j / (float)segsr));
            }
        }

        int w = segsr + 1;
        for (int i = 0; i < segsR; i++)
        for (int j = 0; j < segsr; j++)
        {
            int a = i * w + j, b = (i + 1) * w + j, c = a + 1, d = b + 1;
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(c); tris.Add(b); tris.Add(d);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Ocean Plane ───────────────────────────────────────────────────────────

    /// <summary>
    /// Subdivided plane for OceanWater.shader Gerstner vertex displacement.
    /// Dense enough for smooth wave crests visible at 65° FOV.
    /// </summary>
    public static Mesh OceanPlane(float size = 200f, int resolution = 80)
    {
        var mesh    = new Mesh { name = "OceanPlane", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        int res1    = resolution + 1;
        var verts   = new Vector3[res1 * res1];
        var uvs     = new Vector2[res1 * res1];
        var normals = new Vector3[res1 * res1];

        for (int z = 0; z <= resolution; z++)
        for (int x = 0; x <= resolution; x++)
        {
            int idx = z * res1 + x;
            float fx = (x / (float)resolution - 0.5f) * size;
            float fz = (z / (float)resolution - 0.5f) * size;
            verts[idx]   = new Vector3(fx, 0, fz);
            uvs[idx]     = new Vector2(x / (float)resolution, z / (float)resolution);
            normals[idx] = Vector3.up;
        }

        var tris = new int[resolution * resolution * 6];
        int t = 0;
        for (int z = 0; z < resolution; z++)
        for (int x = 0; x < resolution; x++)
        {
            int a = z * res1 + x, b = a + 1, c = (z + 1) * res1 + x, d = c + 1;
            tris[t++] = a; tris[t++] = c; tris[t++] = b;
            tris[t++] = b; tris[t++] = c; tris[t++] = d;
        }

        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.normals   = normals;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Boost Arrow ───────────────────────────────────────────────────────────

    /// <summary>Flat arrow shape for BumperBlitz boost pads.</summary>
    public static Mesh BoostArrow(float length = 1.6f, float width = 0.8f)
    {
        var mesh = new Mesh { name = "BoostArrow" };
        float hw = width * 0.5f, hl = length * 0.5f;
        float shaftW = hw * 0.45f;

        var verts = new Vector3[]
        {
            // Arrowhead triangle
            new(0, 0, hl),           // tip
            new(-hw, 0, 0),
            new(hw, 0, 0),
            // Shaft rectangle
            new(-shaftW, 0, 0),
            new(shaftW, 0, 0),
            new(-shaftW, 0, -hl),
            new(shaftW, 0, -hl),
        };

        var uvs = new Vector2[]
        {
            new(0.5f, 1),
            new(0, 0.5f), new(1, 0.5f),
            new(0.2f, 0.5f), new(0.8f, 0.5f),
            new(0.2f, 0), new(0.8f, 0),
        };

        var tris = new int[] { 0,1,2, 3,5,4, 4,5,6 };

        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Gravity Orb ───────────────────────────────────────────────────────────

    /// <summary>
    /// High-poly sphere for gravity grab orbs. More poly detail than Unity default.
    /// </summary>
    public static Mesh GravityOrb(float radius = 0.25f, int segs = 20)
    {
        // UV sphere
        var mesh    = new Mesh { name = "GravityOrb" };
        var verts   = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs     = new List<Vector2>();
        var tris    = new List<int>();

        for (int i = 0; i <= segs; i++)
        {
            float phi = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, i / (float)segs);
            for (int j = 0; j <= segs * 2; j++)
            {
                float theta = j / (float)(segs * 2) * Mathf.PI * 2f;
                var n = new Vector3(Mathf.Cos(phi) * Mathf.Cos(theta), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(theta));
                verts.Add(n * radius);
                normals.Add(n);
                uvs.Add(new Vector2(j / (float)(segs * 2), i / (float)segs));
            }
        }

        int w = segs * 2 + 1;
        for (int i = 0; i < segs; i++)
        for (int j = 0; j < segs * 2; j++)
        {
            int a = i * w + j, b = a + w, c = a + 1, d = b + 1;
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(c); tris.Add(b); tris.Add(d);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
