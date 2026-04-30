using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Pulsar.Admin
{
    public class GameBridge
    {
        private readonly object _gameInstance;
        private readonly Stopwatch _uptimeWatch = Stopwatch.StartNew();
        private readonly List<ChatEntry> _chatLog = new List<ChatEntry>();
        private const int MaxChatLogSize = 200;

        public GameBridge(object gameInstance)
        {
            _gameInstance = gameInstance;
        }

        public void Update()
        {
        }

        public ServerStateDto GetServerState()
        {
            var session = MySession.Static;
            if (session == null)
            {
                return new ServerStateDto { IsRunning = false };
            }

            var settings = session.Settings;
            return new ServerStateDto
            {
                IsRunning = true,
                ServerName = session.Name ?? "",
                WorldName = session.Name ?? "",
                PlayersOnline = GetOnlinePlayerCount(),
                MaxPlayers = settings.MaxPlayers,
                SimSpeed = session.SessionSimSpeedServer,
                SimCpuLoad = session.SessionSimSpeedServer > 0 ? (1f / session.SessionSimSpeedServer) * 100f : 0f,
                ServerCpuLoad = 0f,
                UsedPcu = 0,
                TotalPcu = settings.TotalPCU,
                UptimeSeconds = (int)_uptimeWatch.Elapsed.TotalSeconds,
                GameVersion = session.AppVersionFromSave.ToString() ?? "",
                ModsLoaded = session.Mods?.Count ?? 0,
                PluginsLoaded = 0,
            };
        }

        public List<PlayerInfoDto> GetPlayers()
        {
            var session = MySession.Static;
            if (session == null)
                return new List<PlayerInfoDto>();

            var result = new List<PlayerInfoDto>();
            try
            {
                var players = session.Players.GetOnlinePlayers();
                foreach (var player in players)
                {
                    result.Add(new PlayerInfoDto
                    {
                        SteamId = (long)player.Id.SteamId,
                        DisplayName = player.DisplayName ?? "",
                        Faction = GetPlayerFaction(player.Identity?.IdentityId ?? 0),
                        IsAdmin = session.IsUserAdmin(player.Id.SteamId),
                        PingMs = 0,
                    });
                }
            }
            catch
            {
                // Player enumeration may fail during session transitions
            }
            return result;
        }

        public List<ChatEntry> GetChat(int count)
        {
            lock (_chatLog)
            {
                int start = Math.Max(0, _chatLog.Count - count);
                return _chatLog.GetRange(start, _chatLog.Count - start).ToList();
            }
        }

        public void AddChatMessage(string sender, string message)
        {
            lock (_chatLog)
            {
                _chatLog.Add(new ChatEntry
                {
                    Timestamp = DateTime.UtcNow.ToString("HH:mm:ss"),
                    Sender = sender,
                    Message = message,
                });
                while (_chatLog.Count > MaxChatLogSize)
                    _chatLog.RemoveAt(0);
            }
        }

        public bool SendChat(string message)
        {
            try
            {
                var multiplayer = MyMultiplayer.Static;
                if (multiplayer == null)
                    return false;

                multiplayer.SendChatMessage(message, ChatChannel.Global, 0);
                AddChatMessage("Server", message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SaveWorld()
        {
            var session = MySession.Static;
            if (session == null)
                return false;

            return session.Save();
        }

        public bool StopServer()
        {
            try
            {
                var dedicated = typeof(Sandbox.MySandboxGame)
                    .GetProperty("Static", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (dedicated != null)
                {
                    var game = dedicated.GetValue(null);
                    if (game != null)
                    {
                        var exitMethod = game.GetType().GetMethod("ExitThreadSafe");
                        exitMethod?.Invoke(game, null);
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        public bool KickPlayer(long steamId)
        {
            try
            {
                MyMultiplayer.Static?.KickClient((ulong)steamId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool BanPlayer(long steamId)
        {
            try
            {
                MyMultiplayer.Static?.BanClient((ulong)steamId, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UnbanPlayer(long steamId)
        {
            try
            {
                MyMultiplayer.Static?.BanClient((ulong)steamId, false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool PromotePlayer(long steamId)
        {
            var session = MySession.Static;
            if (session == null) return false;
            try
            {
                session.SetUserPromoteLevel((ulong)steamId, MyPromoteLevel.Admin);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DemotePlayer(long steamId)
        {
            var session = MySession.Static;
            if (session == null) return false;
            try
            {
                session.SetUserPromoteLevel((ulong)steamId, MyPromoteLevel.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private int GetOnlinePlayerCount()
        {
            try
            {
                return MySession.Static?.Players?.GetOnlinePlayers()?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private string GetPlayerFaction(long identityId)
        {
            if (identityId == 0) return "";
            try
            {
                var faction = MySession.Static?.Factions?.GetPlayerFaction(identityId);
                return faction?.Tag ?? "";
            }
            catch
            {
                return "";
            }
        }
    }

    public class ServerStateDto
    {
        public bool IsRunning { get; set; }
        public string ServerName { get; set; } = "";
        public string WorldName { get; set; } = "";
        public int PlayersOnline { get; set; }
        public int MaxPlayers { get; set; }
        public float SimSpeed { get; set; }
        public float SimCpuLoad { get; set; }
        public float ServerCpuLoad { get; set; }
        public int UsedPcu { get; set; }
        public int TotalPcu { get; set; }
        public int UptimeSeconds { get; set; }
        public string GameVersion { get; set; } = "";
        public int ModsLoaded { get; set; }
        public int PluginsLoaded { get; set; }
    }

    public class PlayerInfoDto
    {
        public long SteamId { get; set; }
        public string DisplayName { get; set; } = "";
        public string Faction { get; set; } = "";
        public bool IsAdmin { get; set; }
        public int PingMs { get; set; }
    }

    public class ChatEntry
    {
        public string Timestamp { get; set; } = "";
        public string Sender { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
