using System;
using System.Collections.Generic;
using VRageMath;

namespace MagnetarMod
{
    // Build only the part of the surface within sight of the camera. Coordinates are
    // local to the shape until the last step, even for worlds far from the origin.
    internal sealed class RegionSurface
    {
        private const double LegacyDistance = 2100;
        private readonly double viewDistance, patchRadius, pitch;
        private readonly byte skin;
        internal const double CacheTravel = 100;
        private readonly Vector3D center, half;
        private readonly double radius, cellSize;

        private RegionSurface(Vector3D first, Vector3D second, double radius, byte skin, int pitch, double viewDistance)
        {
            this.radius = radius;
            this.skin = skin;
            this.pitch = pitch;
            this.viewDistance = viewDistance;
            // Limit the visible patch in both metres and texture repeats, independently
            // of server draw distance and total region area.
            patchRadius = Math.Min(3500, 6 * pitch);
            center = radius > 0 ? first : first * 0.5 + second * 0.5;
            half = radius > 0 ? new Vector3D(radius) : second * 0.5 - first * 0.5;
            // A small sphere still needs curvature. Stop at world-coordinate precision.
            double precision = Math.Max(0.001, Math.Max(center.AbsMax(), half.AbsMax()) * 1e-14);
            cellSize = Math.Max(precision, radius > 0 ? Math.Min(pitch, radius / 4) : pitch);
        }

        // Channel 48733 supports legacy geometry (v1) and ranged, styled snapshots (v2).
        internal static bool TryDecode(bool fromServer, byte[] data, out RegionSurface[] surfaces)
        {
            surfaces = null;
            if (!fromServer || data == null || data.Length < 2 || data[1] > 4)
                return false;
            bool styled = data[0] == 2;
            int header = styled ? 4 : 2, stride = styled ? 59 : 56;
            if ((!styled && data[0] != 1) || data.Length != header + stride * data[1])
                return false;
            double range = styled ? BitConverter.ToUInt16(data, 2) * 1000d : LegacyDistance;
            if (range > 500000 || (range == 0 && data[1] != 0))
                return false;
            var decoded = new RegionSurface[data[1]];
            int offset = header;
            for (int i = 0; i < decoded.Length; i++, offset += stride)
            {
                Vector3D first = ReadPoint(data, offset), second = ReadPoint(data, offset + 24);
                double r = BitConverter.ToDouble(data, offset + 48);
                if (!Finite(first.LengthSquared()) || !Finite(second.LengthSquared()) ||
                    !Finite(r * r) || r < 0 ||
                    (r == 0 && (first.X >= second.X || first.Y >= second.Y || first.Z >= second.Z)))
                    return false;
                byte skin = styled ? data[offset + 56] : (byte)0;
                int pitch = styled ? BitConverter.ToUInt16(data, offset + 57) : 500;
                if (skin > 16 || pitch < 64 || pitch > 8192)
                    return false;
                decoded[i] = new RegionSurface(first, second, r, skin, pitch, range);
            }
            surfaces = decoded;
            return true;
        }

        private static bool Finite(double n) { return !double.IsNaN(n) && !double.IsInfinity(n); }

        private static Vector3D ReadPoint(byte[] data, int offset)
        {
            return new Vector3D(BitConverter.ToDouble(data, offset),
                BitConverter.ToDouble(data, offset + 8), BitConverter.ToDouble(data, offset + 16));
        }

        internal struct Tile
        {
            public MyQuadD Quad;
            public byte Skin;
            public Vector2 UvMin, UvMax;
            public float PatchFade;
            public double ViewDistance;
            public bool LastColumn, LastRow;
            public BoundingSphereD Bounds;
        }

        internal void Collect(Vector3D camera, List<Tile> tiles)
        {
            Vector3D x = Vector3D.UnitX * half.X, y = Vector3D.UnitY * half.Y, z = Vector3D.UnitZ * half.Z;
            Vector3D eye = camera - center;
            // Anchor the mesh on the closest surface, including deep inside a box.
            // A camera at the sphere center has no unique nearest point.
            Vector3D anchor;
            if (radius > 0)
                anchor = (eye.LengthSquared() > 0 ? Vector3D.Normalize(eye) : Vector3D.UnitZ) * radius;
            else
            {
                anchor = Vector3D.Clamp(eye, -half, half);
                if (eye == anchor)
                {
                    var gap = half - Vector3D.Abs(eye);
                    if (gap.X <= gap.Y && gap.X <= gap.Z) anchor.X = eye.X < 0 ? -half.X : half.X;
                    else if (gap.Y <= gap.Z) anchor.Y = eye.Y < 0 ? -half.Y : half.Y;
                    else anchor.Z = eye.Z < 0 ? -half.Z : half.Z;
                }
            }
            if (Vector3D.Distance(eye, anchor) >= viewDistance + CacheTravel)
                return;
            for (int side = -1; side <= 1; side += 2)
            {
                CollectPatch(x * side, y, z * side, anchor, tiles);
                CollectPatch(y * side, z, x * side, anchor, tiles);
                CollectPatch(z * side, x, y * side, anchor, tiles);
            }
        }

        private Vector3D Project(Vector3D p)
        {
            if (radius == 0)
                return p;
            // Components on the cube are within [-radius, radius]. Scaling first
            // keeps the normalization finite for very large and very small radii.
            return Vector3D.Normalize(new Vector3D(p.X / radius, p.Y / radius, p.Z / radius)) * radius;
        }

        private void CollectPatch(Vector3D mid, Vector3D u, Vector3D v, Vector3D anchor,
            List<Tile> tiles, bool lastColumn = true, bool lastRow = true, double texU = 0, double texV = 0)
        {
            Vector3D projected = Project(mid);
            // Projection from a cube face onto its inscribed sphere cannot increase distance.
            double extent = Math.Sqrt(u.LengthSquared() + v.LengthSquared());
            double patchDistance = Vector3D.Distance(anchor, projected);
            if (patchDistance > patchRadius + extent)
                return;

            double width = u.AbsMax() * 2, height = v.AbsMax() * 2;
            if (Math.Max(width, height) > cellSize)
            {
                // Split textured faces on repeat boundaries before subdividing for sphere
                // curvature. SE billboard samplers clamp UVs, so each leaf stays in [0,1].
                bool horizontal = width >= height;
                double length = horizontal ? width : height;
                bool repeat = skin != 0 && length > pitch && cellSize >= length * 1e-14;
                double cut = repeat ? Math.Max(1, Math.Floor(length / pitch * 0.5)) * pitch : length * 0.5;
                Vector3D axis = horizontal ? u : v;
                var left = axis * (cut / length);
                var right = axis - left;
                if (horizontal)
                {
                    CollectPatch(mid - right, left, v, anchor, tiles, false, lastRow, texU, texV);
                    CollectPatch(mid + left, right, v, anchor, tiles, lastColumn, lastRow,
                        repeat ? 0 : texU + cut / pitch, texV);
                }
                else
                {
                    CollectPatch(mid - right, u, left, anchor, tiles, lastColumn, false, texU, texV);
                    CollectPatch(mid + left, u, right, anchor, tiles, lastColumn, lastRow,
                        texU, repeat ? 0 : texV + cut / pitch);
                }
                return;
            }
            float fade = (float)Math.Min(1, (patchRadius + extent - patchDistance) / (0.3 * patchRadius));
            tiles.Add(new Tile
            {
                Skin = skin,
                UvMin = new Vector2((float)texU, (float)texV),
                UvMax = new Vector2((float)(texU + width / pitch), (float)(texV + height / pitch)),
                ViewDistance = viewDistance,
                PatchFade = fade,
                LastColumn = lastColumn,
                LastRow = lastRow,
                Bounds = new BoundingSphereD(center + projected, extent),
                Quad = new MyQuadD
                {
                    Point0 = center + Project(mid - u - v),
                    Point1 = center + Project(mid + u - v),
                    Point2 = center + Project(mid + u + v),
                    Point3 = center + Project(mid - u + v)
                }
            });
        }
    }
}
