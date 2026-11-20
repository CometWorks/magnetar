using System;
using System.Collections.Generic;
using VRageMath;

namespace MagnetarMod
{
    // Build only the part of the surface within sight of the camera. Coordinates are
    // local to the shape until the last step, even for worlds far from the origin.
    internal sealed class RegionSurface
    {
        internal const double ViewDistance = 2100;
        internal const double CacheTravel = 100;
        private readonly Vector3D center, half;
        private readonly double radius, cellSize;

        private RegionSurface(Vector3D first, Vector3D second, double radius)
        {
            this.radius = radius;
            center = radius > 0 ? first : first * 0.5 + second * 0.5;
            half = radius > 0 ? new Vector3D(radius) : second * 0.5 - first * 0.5;
            // A small sphere still needs curvature. Stop at world-coordinate precision.
            double precision = Math.Max(0.001, Math.Max(center.AbsMax(), half.AbsMax()) * 1e-14);
            cellSize = Math.Max(precision, radius > 0 ? Math.Min(500, radius / 4) : 500);
        }

        // Channel 48733, v1: count, then seven little-endian doubles per surface.
        internal static bool TryDecode(bool fromServer, byte[] data, out RegionSurface[] surfaces)
        {
            surfaces = null;
            if (!fromServer || data == null || data.Length < 2 || data[0] != 1 ||
                data[1] > 4 || data.Length != 2 + 56 * data[1])
                return false;
            var decoded = new RegionSurface[data[1]];
            int offset = 2;
            for (int i = 0; i < decoded.Length; i++, offset += 56)
            {
                Vector3D first = ReadPoint(data, offset), second = ReadPoint(data, offset + 24);
                double r = BitConverter.ToDouble(data, offset + 48);
                if (!Finite(first.LengthSquared()) || !Finite(second.LengthSquared()) ||
                    !Finite(r * r) || r < 0 ||
                    (r == 0 && (first.X >= second.X || first.Y >= second.Y || first.Z >= second.Z)))
                    return false;
                decoded[i] = new RegionSurface(first, second, r);
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
            public bool LastColumn, LastRow;
            public BoundingSphereD Bounds;
        }

        internal void Collect(Vector3D camera, List<Tile> tiles)
        {
            Vector3D x = Vector3D.UnitX * half.X, y = Vector3D.UnitY * half.Y, z = Vector3D.UnitZ * half.Z;
            Vector3D eye = camera - center;
            for (int side = -1; side <= 1; side += 2)
            {
                CollectPatch(x * side, y, z * side, eye, tiles);
                CollectPatch(y * side, z, x * side, eye, tiles);
                CollectPatch(z * side, x, y * side, eye, tiles);
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

        private void CollectPatch(Vector3D mid, Vector3D u, Vector3D v, Vector3D eye,
            List<Tile> tiles, bool lastColumn = true, bool lastRow = true)
        {
            Vector3D projected = Project(mid);
            // Projection from a cube face onto its inscribed sphere cannot increase distance.
            double extent = Math.Sqrt(u.LengthSquared() + v.LengthSquared());
            if (Vector3D.Distance(eye, projected) > ViewDistance + CacheTravel + extent)
                return;

            double width = u.AbsMax(), height = v.AbsMax();
            if (Math.Max(width, height) > cellSize * 0.5)
            {
                if (width >= height)
                {
                    u *= 0.5;
                    CollectPatch(mid - u, u, v, eye, tiles, false, lastRow);
                    CollectPatch(mid + u, u, v, eye, tiles, lastColumn, lastRow);
                }
                else
                {
                    v *= 0.5;
                    CollectPatch(mid - v, u, v, eye, tiles, lastColumn, false);
                    CollectPatch(mid + v, u, v, eye, tiles, lastColumn, lastRow);
                }
                return;
            }
            tiles.Add(new Tile
            {
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
