using System;
using System.Reflection;
using VRage.Plugins;

namespace Pulsar.Admin
{
    public class AdminPlugin : IPlugin
    {
        private HttpServer _server;
        private GameBridge _bridge;

        public void Init(object gameInstance)
        {
            _bridge = new GameBridge(gameInstance);
            _server = new HttpServer(_bridge);
            _server.Start();
        }

        public void Update()
        {
            _bridge.Update();
        }

        public void Dispose()
        {
            _server?.Stop();
            _server = null;
            _bridge = null;
        }
    }
}
