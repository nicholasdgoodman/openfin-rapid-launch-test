using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Fin = Openfin.Desktop;

namespace RapidLaunch.Common
{
    public static class MessagePublisher
    {
        public static void PingAppLauncher()
        {
            if(EnsureConnected())
            {
                OpenFinGlobals.RuntimeInstance.InterApplicationBus.Publish("ping-app-launcher", new object());
            }
        }

        private static bool EnsureConnected()
        {
            if (OpenFinGlobals.RuntimeInstance.IsConnected)
                return true;

            System.Diagnostics.Debug.WriteLine("Not connected... need to connect");

            var tsc = new TaskCompletionSource<bool>();

            EventHandler connectedEventHandler = null;
            EventHandler disconnectedEventHandler = null;
            Fin.OpenFinErrorHandler errorEventHandler = null;

            OpenFinGlobals.RuntimeInstance.Connected += connectedEventHandler = (s, e) =>
            {
                OpenFinGlobals.RuntimeInstance.Connected -= connectedEventHandler;
                System.Diagnostics.Debug.WriteLine($"Runtime Connected!");
                tsc.SetResult(true);
            };

            OpenFinGlobals.RuntimeInstance.Disconnected += disconnectedEventHandler = (s, e) =>
            {
                OpenFinGlobals.RuntimeInstance.Disconnected -= disconnectedEventHandler;
                System.Diagnostics.Debug.WriteLine($"Runtime Disconnected!");
                tsc.SetResult(false);
            };

            OpenFinGlobals.RuntimeInstance.Error += errorEventHandler = (s, e) =>
            {
                OpenFinGlobals.RuntimeInstance.Error -= errorEventHandler;
                System.Diagnostics.Debug.WriteLine($"Runtime Error!");
            };

            OpenFinGlobals.RuntimeInstance.Connect(() => { });

            System.Diagnostics.Debug.WriteLine("Connect called. Awaiting result.");

            return tsc.Task.Result;
        }
    }
}
