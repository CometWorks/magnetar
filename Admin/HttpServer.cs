using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Pulsar.Admin
{
    public class HttpServer
    {
        private readonly GameBridge _bridge;
        private readonly HttpListener _listener;
        private Thread _thread;
        private volatile bool _running;
        private readonly int _port;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };

        public HttpServer(GameBridge bridge, int port = 9000)
        {
            _bridge = bridge;
            _port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
        }

        public void Start()
        {
            _running = true;
            _listener.Start();
            _thread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "PulsarAdminHttp",
            };
            _thread.Start();
            Log($"Admin HTTP server started on port {_port}");
        }

        public void Stop()
        {
            _running = false;
            try
            {
                _listener.Stop();
            }
            catch
            {
            }
            Log("Admin HTTP server stopped");
        }

        private void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException) when (!_running)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Listener error: {ex.Message}");
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();
                return;
            }

            try
            {
                var path = request.Url.AbsolutePath.TrimEnd('/');
                var method = request.HttpMethod;

                object result = null;
                int statusCode = 200;

                if (path == "/api/state" && method == "GET")
                {
                    result = _bridge.GetServerState();
                }
                else if (path == "/api/players" && method == "GET")
                {
                    result = _bridge.GetPlayers();
                }
                else if (path == "/api/chat" && method == "GET")
                {
                    int count = 50;
                    var countParam = request.QueryString["count"];
                    if (countParam != null)
                        int.TryParse(countParam, out count);
                    result = _bridge.GetChat(count);
                }
                else if (path == "/api/chat" && method == "POST")
                {
                    var body = ReadBody(request);
                    var msg = JsonConvert.DeserializeAnonymousType(body, new { message = "" });
                    bool ok = _bridge.SendChat(msg?.message ?? "");
                    result = new { status = ok ? "sent" : "failed" };
                }
                else if (path == "/api/save" && method == "POST")
                {
                    bool ok = _bridge.SaveWorld();
                    result = new { status = ok ? "saved" : "failed" };
                }
                else if (path == "/api/stop" && method == "POST")
                {
                    bool ok = _bridge.StopServer();
                    result = new { status = ok ? "stopping" : "failed" };
                }
                else if (path.StartsWith("/api/players/") && method == "POST")
                {
                    result = HandlePlayerAction(path);
                }
                else if (path == "/api/session-settings" && method == "POST")
                {
                    result = new { status = "not_implemented" };
                    statusCode = 501;
                }
                else
                {
                    result = new { error = "not_found" };
                    statusCode = 404;
                }

                response.StatusCode = statusCode;
                WriteJson(response, result);
            }
            catch (Exception ex)
            {
                Log($"Request error: {ex.Message}");
                response.StatusCode = 500;
                WriteJson(response, new { error = ex.Message });
            }
        }

        private object HandlePlayerAction(string path)
        {
            // /api/players/{steamId}/{action}
            var parts = path.Split('/');
            if (parts.Length < 5)
                return new { status = "invalid_path" };

            if (!long.TryParse(parts[3], out long steamId))
                return new { status = "invalid_steam_id" };

            string action = parts[4];
            bool ok;
            switch (action)
            {
                case "kick":
                    ok = _bridge.KickPlayer(steamId);
                    return new { status = ok ? "kicked" : "failed" };
                case "ban":
                    ok = _bridge.BanPlayer(steamId);
                    return new { status = ok ? "banned" : "failed" };
                case "unban":
                    ok = _bridge.UnbanPlayer(steamId);
                    return new { status = ok ? "unbanned" : "failed" };
                case "promote":
                    ok = _bridge.PromotePlayer(steamId);
                    return new { status = ok ? "promoted" : "failed" };
                case "demote":
                    ok = _bridge.DemotePlayer(steamId);
                    return new { status = ok ? "demoted" : "failed" };
                default:
                    return new { status = "unknown_action" };
            }
        }

        private static string ReadBody(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }

        private static void WriteJson(HttpListenerResponse response, object data)
        {
            response.ContentType = "application/json; charset=utf-8";
            string json = JsonConvert.SerializeObject(data, JsonSettings);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }

        private static void Log(string message)
        {
            Console.WriteLine($"[PulsarAdmin] {message}");
        }
    }
}
