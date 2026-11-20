using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace MagnetarMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class RegionBarriers : MySessionComponentBase
    {
        private const ushort Channel = 48733;
        private static readonly MyStringId Square = MyStringId.GetOrCompute("Square");
        // IDs match the Essentials BarrierSkin enum. Grid (zero) uses the original lattice.
        private static readonly MyStringId[] Skins = new MyStringId[]
        {
            Square,
            MyStringId.GetOrCompute("MagnetarRegionDefault"),
            MyStringId.GetOrCompute("MagnetarRegionAura"),
            MyStringId.GetOrCompute("MagnetarRegionOrganic"),
            MyStringId.GetOrCompute("MagnetarRegionRain"),
            MyStringId.GetOrCompute("MagnetarRegionDots"),
            MyStringId.GetOrCompute("MagnetarRegionDisco"),
            MyStringId.GetOrCompute("MagnetarRegionNoise"),
            MyStringId.GetOrCompute("MagnetarRegionHexagon"),
            MyStringId.GetOrCompute("MagnetarRegionLines"),
            MyStringId.GetOrCompute("MagnetarRegionDigital"),
            MyStringId.GetOrCompute("MagnetarRegionGloura"),
            MyStringId.GetOrCompute("MagnetarRegionClang"),
            MyStringId.GetOrCompute("MagnetarRegionVoronoi"),
            MyStringId.GetOrCompute("MagnetarRegionRestricted"),
            MyStringId.GetOrCompute("MagnetarRegionKeenSWH"),
            MyStringId.GetOrCompute("MagnetarRegionDisabled"),
        };
        private readonly List<RegionSurface.Tile> tiles = new List<RegionSurface.Tile>();
        private RegionSurface[] surfaces;
        private Vector3D? lastCamera;
        private int receivedFrame;
        private bool styledSnapshots;
        private bool registered;

        public override void BeforeStart()
        {
            if (MyAPIGateway.Utilities.IsDedicated || MyAPIGateway.Multiplayer == null)
                return;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Channel, Receive);
            registered = true;
        }

        protected override void UnloadData()
        {
            if (registered && MyAPIGateway.Multiplayer != null)
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Channel, Receive);
            registered = false;
            surfaces = null;
            tiles.Clear();
        }

        private void Receive(ushort channel, byte[] data, ulong sender, bool fromServer)
        {
            RegionSurface[] next;
            if (!RegionSurface.TryDecode(fromServer, data, out next))
                return;
            MyAPIGateway.Utilities.InvokeOnGameThread(delegate
            {
                if (!registered || MyAPIGateway.Session == null)
                    return;
                // Once v2 is in use, ignore the compatibility copy sent before each
                // refresh. Always accept the shared v1 clear, including on downgrade.
                if (data[0] == 1 && next.Length > 0 && styledSnapshots)
                    return;
                styledSnapshots = data[0] == 2;
                surfaces = next.Length == 0 ? null : next;
                lastCamera = null;
                receivedFrame = MyAPIGateway.Session.GameplayFrameCounter;
                tiles.Clear();
            }, "MagnetarMod.RegionBarriers");
        }

        public override void Draw()
        {
            if (surfaces == null)
                return;
            var session = MyAPIGateway.Session;
            if (session == null || unchecked(session.GameplayFrameCounter - receivedFrame) > 360)
            {
                styledSnapshots = false;
                surfaces = null;
                tiles.Clear();
                return;
            }
            var camera = session.Camera;
            if (camera == null)
                return;
            var eye = camera.Position;
            if (!lastCamera.HasValue || Vector3D.DistanceSquared(eye, lastCamera.Value) >=
                RegionSurface.CacheTravel * RegionSurface.CacheTravel)
            {
                tiles.Clear();
                foreach (var surface in surfaces)
                    surface.Collect(eye, tiles);
                lastCamera = eye;
            }
            foreach (var tile in tiles)
            {
                var bounds = tile.Bounds;
                double distance = Vector3D.Distance(eye, bounds.Center);
                // ModAPI frustum tests end at the world's entity view distance. For distant
                // patches test the same angular bounds closer in, then draw at true depth.
                var probe = bounds;
                if (distance > camera.FarPlaneDistance * 0.5)
                {
                    double factor = camera.FarPlaneDistance * 0.5 / distance;
                    probe.Center = eye + (bounds.Center - eye) * factor;
                    probe.Radius *= factor;
                }
                if (!camera.IsInFrustum(ref probe))
                    continue;
                float fade = tile.PatchFade * (float)Math.Min(1, (tile.ViewDistance - distance) / Math.Min(1500, tile.ViewDistance * 0.2));
                if (fade <= 0)
                    continue;
                var quad = tile.Quad;
                if (tile.Skin != 0)
                {
                    var material = Skins[tile.Skin];
                    var tint = new Vector4(0.14f, 0.80f, 0.95f, 0.62f) * fade;
                    var lo = tile.UvMin;
                    var hi = tile.UvMax;
                    var n = Vector3.Zero; // Unlit billboard material.
                    var pos = bounds.Center;
                    // Keen marks the per-billboard tint overload "Only for modders".
                    MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point1, quad.Point2,
                        n, n, n, lo, new Vector2(hi.X, lo.Y), hi, material, uint.MaxValue, pos, tint);
                    MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point2, quad.Point3,
                        n, n, n, lo, hi, new Vector2(lo.X, hi.Y), material, uint.MaxValue, pos, tint);
                    continue;
                }
                var fill = new Vector4(0.015f, 0.12f, 0.16f, 0.16f) * fade;
                var line = new Vector4(0.06f, 0.65f, 0.8f, 0.8f) * fade;
                MyTransparentGeometry.AddQuad(Square, ref quad, fill, ref bounds.Center);
                // Keep the lattice legible against a starfield at kilometre distances.
                float thickness = (float)Math.Min(bounds.Radius * 0.05, Math.Max(0.12, distance * 0.003));
                MySimpleObjectDraw.DrawLine(quad.Point0, quad.Point1, Square, ref line, thickness);
                MySimpleObjectDraw.DrawLine(quad.Point0, quad.Point3, Square, ref line, thickness);
                // Interior tiles share lines. The far edges of a face have no next tile.
                if (tile.LastColumn)
                    MySimpleObjectDraw.DrawLine(quad.Point1, quad.Point2, Square, ref line, thickness);
                if (tile.LastRow)
                    MySimpleObjectDraw.DrawLine(quad.Point3, quad.Point2, Square, ref line, thickness);
            }
        }
    }
}
