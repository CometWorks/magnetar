using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MagnetarMod;
using VRageMath;
using Xunit;

public class RegionSurfaceTests
{
    private static byte[] Packet(Vector3D first, Vector3D second, double radius)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)1);
        writer.Write((byte)1);
        foreach (var value in new[] { first.X, first.Y, first.Z, second.X, second.Y, second.Z, radius })
            writer.Write(value);
        return stream.ToArray();
    }

    [Fact]
    public void ReceiverRequiresServerAndValidCompleteSnapshots()
    {
        var data = Packet(Vector3D.Zero, Vector3D.One, 0);
        Assert.False(RegionSurface.TryDecode(false, data, out _));
        Assert.False(RegionSurface.TryDecode(true, data[..^1], out _));
        Assert.False(RegionSurface.TryDecode(true, new byte[] { 2, 0 }, out _));
        Assert.False(RegionSurface.TryDecode(true, new byte[] { 1, 5 }.Concat(new byte[280]).ToArray(), out _));
        Assert.False(RegionSurface.TryDecode(true, Packet(Vector3D.One, Vector3D.Zero, 0), out _));
        Assert.False(RegionSurface.TryDecode(true, Packet(Vector3D.Zero, Vector3D.One, double.NaN), out _));
        Assert.False(RegionSurface.TryDecode(true, Packet(new Vector3D(double.PositiveInfinity), Vector3D.One, 1), out _));
        Assert.True(RegionSurface.TryDecode(true, new byte[] { 1, 0 }, out var empty));
        Assert.Empty(empty);
    }

    [Fact]
    public void SubdividedBoxFacesIncludeTheirOuterGridLines()
    {
        var size = new Vector3D(600);
        Assert.True(RegionSurface.TryDecode(true, Packet(-size, size, 0), out var surfaces));
        var tiles = new List<RegionSurface.Tile>();
        surfaces[0].Collect(Vector3D.Zero, tiles);
        var face = tiles.Where(tile => tile.Bounds.Center.X == size.X).ToArray();
        Assert.Equal(16, face.Length);
        Assert.Equal(4, face.Count(tile => tile.LastRow));
        Assert.Equal(4, face.Count(tile => tile.LastColumn));
        foreach (var tile in face)
        {
            Assert.Equal(tile.Quad.Point1.Y == size.Y, tile.LastColumn);
            Assert.Equal(tile.Quad.Point3.Z == size.Z, tile.LastRow);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void LocalTilesCoverTheBoundaryEvenFarFromAnEdge(bool sphere, bool diagonal)
    {
        var center = new Vector3D(1e9, -2e9, 3e9);
        const double size = 250000;
        var packet = sphere ? Packet(center, center, size) : Packet(center - size, center + size, 0);
        Assert.True(RegionSurface.TryDecode(true, packet, out var surfaces));
        var direction = diagonal ? Vector3D.Normalize(new Vector3D(1, -2, 2)) : Vector3D.UnitX;
        var target = center + direction * size;
        var tiles = new List<RegionSurface.Tile>();
        foreach (int offset in new[] { -1200, 1200 })
        {
            tiles.Clear();
            surfaces[0].Collect(target + direction * offset, tiles);
            Assert.InRange(tiles.Count, 1, 2500);
            Assert.Contains(tiles, t => Vector3D.Distance(target, t.Bounds.Center) <= t.Bounds.Radius);
            foreach (var tile in tiles)
            {
                var q = tile.Quad;
                foreach (var point in new[] { q.Point0, q.Point1, q.Point2, q.Point3 })
                    Assert.InRange(sphere ? Math.Abs(Vector3D.Distance(center, point) - size) :
                        Math.Abs(point.X - target.X), 0, 0.00001);
                if (sphere)
                    Assert.InRange(size - Vector3D.Distance((q.Point0 + q.Point2) * 0.5, center), -0.00001, 1);
            }
        }
        tiles.Clear();
        surfaces[0].Collect(center, tiles);
        Assert.Empty(tiles);
    }
}
