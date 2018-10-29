using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;

using RapidLaunch.Common;

namespace RapidLaunch.AppLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Process> spawnedProcesses = new List<Process>();
        private IMessageBus mMessageBus;

        public MainWindow()
        {
            InitializeComponent();

            OpenFinGlobals.RuntimeInstance.Connected += OpenFinRuntime_Connected;
            OpenFinGlobals.RuntimeInstance.Disconnected += OpenFinRuntime_Disconnected;

            OpenFinGlobals.RuntimeInstance.Connect(() => 
            {
                mMessageBus = new MessageBus();
            });
        }

        private void OpenFinRuntime_Connected(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectionStatusText.Text = "OpenFin Connected";
                SpawnButton.IsEnabled = true;
                SpawnAdHocButton.IsEnabled = true;
                ArrangeButton.IsEnabled = true;
                CloseButton.IsEnabled = true;
                TerminateButton.IsEnabled = true;
            });
        }

        private void OpenFinRuntime_Disconnected(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (ConnectionStatusText.Text == "OpenFin Connected")
                {
                    ConnectionStatusText.Text = "OpenFin Disconnected";
                }
                else
                {
                    ConnectionStatusText.Text = "OpenFin Connect Failure";
                }
            });

        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SpawnButton_Click(object sender, RoutedEventArgs e)
        {
            var wpfAppCount = int.Parse(AppCountText.Text);
            var embeddedViewCount = int.Parse(EmbeddedViewCountText.Text);
            var delay = int.Parse(DelayText.Text);

            var spawnAppsTask = new Task(() => SpawnChildApps(wpfAppCount, embeddedViewCount, delay));
            spawnAppsTask.Start();
        }

        private void SpawnChildApps(int wpfAppCount, int embeddedViewCount, int delay)
        {
            var appsToSpawn = wpfAppCount;
            var embeddedViewToSpawn = embeddedViewCount > wpfAppCount ? wpfAppCount : embeddedViewCount;

            var rand = new Random(Environment.TickCount);

            // Randomly shuffle each app if it gets an embedded view
            var appsShouldEmbed = Enumerable
                .Range(0, appsToSpawn)
                .Select(n => n < embeddedViewToSpawn)
                .OrderBy(b => rand.Next())
                .ToArray();

            foreach (var appShouldEmbed in appsShouldEmbed)
            {
                var tsc = new TaskCompletionSource<Process>();

                if (appShouldEmbed)
                {
                    tsc = SpawnEmbeddedApp();
                }
                else
                {
                    tsc.SetResult(Process.Start(new ProcessStartInfo()
                    {
                        FileName = "SpawnedApp.exe",
                        UseShellExecute = false
                    }));
                }

                var spawnedProcess = tsc.Task.Result;

                spawnedProcesses.Add(spawnedProcess);
                spawnedProcess.Exited += SpawnedProcess_Exited;

                Task.Delay(delay).Wait();
            }
        }

        private void AdhocSpawnButton_Click(object sender, RoutedEventArgs e)
        {
            var tsc = SpawnEmbeddedApp();
            var spawnedProcess = tsc.Task.Result;

            spawnedProcesses.Add(spawnedProcess);
            spawnedProcess.Exited += SpawnedProcess_Exited;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private void ArrangeButton_Click(object sender, RoutedEventArgs e)
        {
            for(int n = 0; n < spawnedProcesses.Count; n++)
            {
                var row = n % 5;
                var col = n / 5;
                var xf = 280;
                var yf = 160;

                MoveWindow(spawnedProcesses[n].MainWindowHandle, xf * col, yf * row, xf, yf, true);
            }
        }

        private void SpawnedProcess_Exited(object sender, EventArgs e)
        {
            spawnedProcesses.Remove(sender as Process);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            foreach(var spawnedProcess in spawnedProcesses.ToArray())
            {
                spawnedProcess.CloseMainWindow();
                spawnedProcesses.Remove(spawnedProcess);
            }
        }

        private void TerminateButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var spawnedProcess in spawnedProcesses.ToArray())
            {
                spawnedProcess.Kill();
                spawnedProcesses.Remove(spawnedProcess);
            }
        }

        #region Helpers
        private TaskCompletionSource<Process> SpawnEmbeddedApp()
        {
            var tsc = new TaskCompletionSource<Process>();
            var appUuid = Guid.NewGuid().ToString();

            var appOptions = new Openfin.Desktop.ApplicationOptions(
                name: appUuid,
                uuid: appUuid,
                url: OpenFinGlobals.DefaultAppUrl)
            {
                NonPersistent = false
            };

            Openfin.Desktop.Application app = null;
            app = new Openfin.Desktop.Application(appOptions, OpenFinGlobals.RuntimeInstance.DesktopConnection,
            ack =>
            {
                app.Run(null);
            });

            tsc.SetResult(Process.Start(new ProcessStartInfo()
            {
                FileName = "SpawnedApp.exe",
                Arguments = appUuid,
                UseShellExecute = false
            }));

            app.Closed += (s, e) =>
            {
                mMessageBus.Publish("sometopic", $"{app.Uuid} is closed");
            };

            mMessageBus.Publish("sometopic", $"{app.Uuid} is starting...");

            return tsc;
        }
        #endregion
    }
}
