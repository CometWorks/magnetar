using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace MagnetarMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public sealed class PlayerTeleporter : MySessionComponentBase
    {
        private const string Subtype = "MagnetarMod_PlayerTeleporter";
        private const string ConfigFile = "TeleporterRoutes.xml";
        private const int TicksPerSecond = 60;
        private const int BoardingSeconds = 10;
        private const ushort DepartureChannel = 48732;
        public const long CompletedMessage = 0x4D47545000000001;
        // Metres along the block's local Up, relative to the large-grid cell centre.
        private const double Deck = -0.946;
        private readonly Dictionary<long, Route> routes = new Dictionary<long, Route>();
        private readonly List<IMyPlayer> players = new List<IMyPlayer>();
        private readonly Dictionary<long, Boarding> boarding = new Dictionary<long, Boarding>();
        private readonly List<Arrival> pending = new List<Arrival>();
        private long frame;
        private long nextTicket;

        private sealed class Boarding
        {
            public IMyCharacter Character;
            public IMyFunctionalBlock Pad;
            // Zero means this visit cannot start another trip until the player leaves.
            public long DepartureFrame;
            public long Ticket;
        }

        public sealed class Routing
        {
            public Route[] Routes = new Route[0];
        }

        public sealed class Route
        {
            public long From;
            public long To;
            public bool AdminOnly;
        }

        private sealed class Arrival
        {
            public ulong Sender;
            public long Identity;
            public IMyCharacter Character;
            public long Source;
            public IMyFunctionalBlock Destination;
            public Vector3D LocalPosition;
            public long Frame;
        }

        public override void BeforeStart()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(DepartureChannel, ReceiveDeparture);
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;
            try
            {
                var files = MyAPIGateway.Utilities;
                if (!files.FileExistsInWorldStorage(ConfigFile, typeof(PlayerTeleporter)))
                {
                    using (var writer = files.WriteFileInWorldStorage(ConfigFile, typeof(PlayerTeleporter)))
                        writer.Write(files.SerializeToXML(new Routing()));
                }
                using (var reader = files.ReadFileInWorldStorage(ConfigFile, typeof(PlayerTeleporter)))
                {
                    var config = files.SerializeFromXML<Routing>(reader.ReadToEnd());
                    foreach (var route in config.Routes)
                    {
                        if (route.From == 0 || route.To == 0 || route.From == route.To)
                            throw new InvalidOperationException("Routes must link two different pad entity IDs.");
                        routes.Add(route.From, route);
                    }
                }
            }
            catch (Exception e)
            {
                routes.Clear();
                MyLog.Default.WriteLine("[MagnetarMod] Teleporter routes disabled: " + e.Message);
            }
            MyVisualScriptLogicProvider.PlayerDisconnected += Disconnected;
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Multiplayer == null)
                return;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(DepartureChannel, ReceiveDeparture);
            if (MyAPIGateway.Multiplayer.IsServer)
                MyVisualScriptLogicProvider.PlayerDisconnected -= Disconnected;
        }

        private void Disconnected(long identity)
        {
            boarding.Remove(identity);
            pending.RemoveAll(a => a.Identity == identity);
        }

        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;
            frame++;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var arrival = pending[i];
                if (frame - arrival.Frame < 3)
                    continue;
                pending.RemoveAt(i);
                Confirm(arrival);
            }

            // Discover entrants six times a second. Recheck a boarding character every tick.
            bool scan = frame % 10 == 0;
            if (scan)
            {
                players.Clear();
                MyAPIGateway.Players.GetPlayers(players, p => !p.IsBot);
            }
            foreach (var player in players)
                UpdateBoarding(player, scan);
        }

        private void UpdateBoarding(IMyPlayer player, bool scan)
        {
            if (!OnFoot(player))
            {
                boarding.Remove(player.IdentityId);
                return;
            }

            Boarding visit;
            if (!boarding.TryGetValue(player.IdentityId, out visit) || visit.Character != player.Character)
            {
                if (!scan)
                    return;
                // Require a step off if the character first appears on a pad.
                boarding[player.IdentityId] = new Boarding
                {
                    Character = player.Character,
                    Pad = FindPad(player.Character.GetPosition())
                };
                return;
            }

            if (!scan && visit.Pad == null)
                return;
            var feet = player.Character.GetPosition();
            if (visit.Pad != null)
            {
                var held = visit.DepartureFrame == 0;
                if (!visit.Pad.MarkedForClose && OnDisc(visit.Pad, feet, held ? 2.5 : 0.2)
                    && (held || (visit.Pad.IsWorking && Grounded(player.Character))))
                {
                    if (!held)
                        Countdown(player, visit);
                    return;
                }
                if (!held)
                    Tell(player, "Teleport cancelled.");
                visit.Pad = null;
                visit.DepartureFrame = 0;
                visit.Ticket = 0;
            }

            if (!scan || !Grounded(player.Character))
                return;
            visit.Pad = FindPad(feet);
            if (visit.Pad == null || Destination(player, visit.Pad) == null)
                return;
            visit.DepartureFrame = frame + BoardingSeconds * TicksPerSecond;
            Countdown(player, visit);
        }

        private void Countdown(IMyPlayer player, Boarding visit)
        {
            long remaining = visit.DepartureFrame - frame;
            if (remaining > 0)
            {
                if (remaining % TicksPerSecond == 0)
                    Tell(player, "Teleport in " + remaining / TicksPerSecond + " s. Step off to cancel.", 1000);
                return;
            }

            if (remaining == 0)
            {
                if (player == MyAPIGateway.Session.Player)
                    Depart(player, visit);
                else
                {
                    // After a network loss the engine may list the old player until timeout.
                    // Ask this client for its departure request only when boarding is complete.
                    visit.Ticket = ++nextTicket;
                    var challenge = new byte[16];
                    BitConverter.GetBytes(visit.Pad.EntityId).CopyTo(challenge, 0);
                    BitConverter.GetBytes(visit.Ticket).CopyTo(challenge, 8);
                    MyAPIGateway.Multiplayer.SendMessageTo(DepartureChannel, challenge, player.SteamUserId);
                }
            }
            else if (remaining <= -5 * TicksPerSecond)
            {
                visit.Ticket = 0;
                visit.DepartureFrame = 0;
                Tell(player, "Teleport cancelled. Step off and board again.");
            }
        }

        private void ReceiveDeparture(ushort channel, byte[] data, ulong sender, bool fromServer)
        {
            bool server = MyAPIGateway.Multiplayer.IsServer;
            if (data == null || fromServer == server || data.Length != (server ? 8 : 16))
                return;
            MyAPIGateway.Utilities.InvokeOnGameThread(delegate
            {
                if (server)
                {
                    var player = PlayerFor(sender);
                    Boarding visit;
                    if (player != null && boarding.TryGetValue(player.IdentityId, out visit) && visit.Ticket != 0
                        && visit.Ticket == BitConverter.ToInt64(data, 0) && player.Character == visit.Character
                        && OnFoot(player) && Grounded(player.Character) && !visit.Pad.MarkedForClose
                        && OnDisc(visit.Pad, player.Character.GetPosition(), 0.2))
                        Depart(player, visit);
                    return;
                }

                var local = MyAPIGateway.Session.Player;
                IMyEntity entity;
                MyAPIGateway.Entities.TryGetEntityById(BitConverter.ToInt64(data, 0), out entity);
                var pad = Pad(entity);
                if (local != null && pad != null && OnFoot(local) && Grounded(local.Character)
                    && OnDisc(pad, local.Character.GetPosition(), 0.2))
                    MyAPIGateway.Multiplayer.SendMessageToServer(DepartureChannel,
                        BitConverter.GetBytes(BitConverter.ToInt64(data, 8)));
            }, "MagnetarMod.Teleporter.Departure");
        }

        private void Depart(IMyPlayer player, Boarding visit)
        {
            visit.Ticket = 0;
            visit.DepartureFrame = 0;
            var destination = Destination(player, visit.Pad);
            if (destination != null)
                Transfer(player, visit, destination);
        }

        private static bool Grounded(IMyCharacter character)
        {
            var mode = character.CurrentMovementState.GetMode();
            return mode == MyCharacterMovement.Standing || mode == MyCharacterMovement.Crouching;
        }

        private static bool OnFoot(IMyPlayer player)
        {
            var character = player.Character;
            return character != null && !character.IsDead && !character.MarkedForClose
                && character.Physics != null && player.Controller != null && player.Controller.ControlledEntity == character
                && character.Parent == null;
        }

        private static IMyFunctionalBlock Pad(IMyEntity entity)
        {
            var block = entity as IMyFunctionalBlock;
            return block != null && !block.MarkedForClose && block.CubeGrid.Physics != null
                && block.BlockDefinition.TypeId == typeof(MyObjectBuilder_FunctionalBlock)
                && block.BlockDefinition.SubtypeName == Subtype ? block : null;
        }

        private static bool OnDisc(IMyFunctionalBlock pad, Vector3D feet, double height)
        {
            Vector3D p = Vector3D.Transform(feet, MatrixD.Invert(pad.WorldMatrix));
            return p.X * p.X + p.Z * p.Z <= 0.9 * 0.9 && p.Y >= Deck - 0.18 && p.Y <= Deck + height;
        }

        private static IMyFunctionalBlock FindPad(Vector3D feet)
        {
            var region = new BoundingSphereD(feet, 2);
            foreach (var entity in MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref region))
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null)
                    continue;
                var slim = grid.GetCubeBlock(grid.WorldToGridInteger(feet));
                var pad = slim == null ? null : Pad(slim.FatBlock);
                if (pad != null && OnDisc(pad, feet, 0.2))
                    return pad;
            }
            return null;
        }

        private static IMyPlayer PlayerFor(ulong sender)
        {
            var players = MyAPIGateway.Players;
            var player = players.TryGetIdentityId(players.TryGetIdentityId(sender));
            return player != null && player.SteamUserId == sender ? player : null;
        }

        private IMyFunctionalBlock Destination(IMyPlayer player, IMyFunctionalBlock source)
        {
            Route route;
            if (!source.IsWorking || !source.HasPlayerAccess(player.IdentityId))
                Tell(player, "Source pad unavailable or access denied.");
            else if (!routes.TryGetValue(source.EntityId, out route))
                Tell(player, "No route configured for pad " + source.EntityId + ".");
            else if (route.AdminOnly && player.PromoteLevel < MyPromoteLevel.Admin)
                Tell(player, "This teleporter route requires an administrator.");
            else
            {
                IMyEntity entity;
                MyAPIGateway.Entities.TryGetEntityById(route.To, out entity);
                var destination = Pad(entity);
                if (destination != null && destination.IsWorking && destination.HasPlayerAccess(player.IdentityId))
                    return destination;
                Tell(player, "Destination pad unavailable or access denied.");
            }
            return null;
        }

        private void Transfer(IMyPlayer player, Boarding visit, IMyFunctionalBlock destination)
        {
            var character = player.Character;
            // Character facing is controlled by its client. Keep it, including in zero gravity,
            // and test the volume the engineer will actually occupy above this pad.
            var pose = character.WorldMatrix;
            var pad = destination.WorldMatrix;
            var box = character.LocalAABB;
            var half = box.HalfExtents;
            float slice = half.Y / 3;
            float radius = new Vector3(half.X, slice, half.Z).Length() + 0.025f;
            var step = pose.Up * (2 * slice);
            var first = Vector3D.TransformNormal(box.Center, pose) - step;
            var lowest = Math.Min(Vector3D.Dot(first, pad.Up), Vector3D.Dot(first + 2 * step, pad.Up));
            pose.Translation = pad.Translation + pad.Up * (Deck + radius + 0.07 - lowest);
            for (int i = 0; i < 3; i++)
            {
                var world = pose.Translation + first + i * step;
                var free = MyAPIGateway.Entities.FindFreePlace(world, radius, 0);
                if (!free.HasValue || Vector3D.DistanceSquared(free.Value, world) > 0.0001)
                {
                    Tell(player, "Arrival space obstructed.");
                    return;
                }
            }
            var local = Vector3D.Transform(pose.Translation, MatrixD.Invert(pad));
            character.Teleport(pose);
            character.Physics.LinearVelocity = destination.CubeGrid.Physics.GetVelocityAtPoint(pose.Translation);
            character.Physics.AngularVelocity = Vector3.Zero;
            pending.Add(new Arrival { Sender = player.SteamUserId, Identity = player.IdentityId, Character = character,
                Source = visit.Pad.EntityId, Destination = destination, LocalPosition = local, Frame = frame });
            visit.Pad = destination;
        }

        private static void Confirm(Arrival arrival)
        {
            var player = PlayerFor(arrival.Sender);
            if (player == null || player.IdentityId != arrival.Identity || !OnFoot(player)
                || player.Character != arrival.Character || arrival.Destination.MarkedForClose)
                return;
            var expected = Vector3D.Transform(arrival.LocalPosition, arrival.Destination.WorldMatrix);
            if (Vector3D.DistanceSquared(player.Character.GetPosition(), expected) > 0.35 * 0.35)
            {
                Tell(player, "Teleporter arrival could not be confirmed.");
                return;
            }
            // Local server API only. Cluster consumers decide the meaning of this directed route.
            MyAPIGateway.Utilities.SendModMessage(CompletedMessage,
                new MyTuple<ulong, long, long, long>(arrival.Sender, arrival.Identity, arrival.Source, arrival.Destination.EntityId));
            MyLog.Default.WriteLine("[MagnetarMod] Teleporter arrival " + arrival.Identity + ": "
                + arrival.Source + " -> " + arrival.Destination.EntityId);
            Tell(player, "Transfer complete.");
        }

        private static void Tell(IMyPlayer player, string text, int duration = 2500)
        {
            if (player == MyAPIGateway.Session.Player)
                MyAPIGateway.Utilities.ShowNotification(text, duration);
            else
                MyVisualScriptLogicProvider.ShowNotification(text, duration, "White", player.IdentityId);
        }
    }
}
