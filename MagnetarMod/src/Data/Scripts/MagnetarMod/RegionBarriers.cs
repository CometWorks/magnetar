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
        private readonly List<RegionSurface.Tile> tiles = new List<RegionSurface.Tile>();
        private RegionSurface[] surfaces;
        private Vector3D? lastCamera;
        private int receivedFrame;
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
                if (!camera.IsInFrustum(ref bounds))
                    continue;
                double distance = Vector3D.Distance(eye, bounds.Center);
                float fade = (float)Math.Min(1, (RegionSurface.ViewDistance - distance) / 500);
                if (fade <= 0)
                    continue;
                var quad = tile.Quad;
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
