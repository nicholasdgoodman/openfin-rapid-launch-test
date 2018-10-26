using Openfin.Desktop;
using System;
using System.Threading.Tasks;

namespace RapidLaunch.Common
{
    public class MessageBus: IMessageBus
    {
        private readonly Runtime mRuntime;

        public MessageBus()
        {
            mRuntime = Runtime.GetRuntimeInstance(OpenFinGlobals.RuntimeInstance.Options);

            ConnectAsync();
        }

        private Task ConnectAsync()
        {
            var tcs = new TaskCompletionSource<int>();

            mRuntime.Connect(() =>
            {
                tcs.SetResult(1);
            });

            return tcs.Task;
        }

        public void Publish<T>(string topic, T message)
        {
            if (EnsureConnected())
            {
                InterApplicationBus.Publish(mRuntime, topic, message);
            }
        }

        private bool EnsureConnected()
        {
            if (OpenFinGlobals.RuntimeInstance.IsConnected)
                return true;

            System.Diagnostics.Debug.WriteLine("Not connected... need to connect");

            var tsc = new TaskCompletionSource<bool>();

            EventHandler connectedEventHandler = null;
            EventHandler disconnectedEventHandler = null;
            OpenFinErrorHandler errorEventHandler = null;

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
