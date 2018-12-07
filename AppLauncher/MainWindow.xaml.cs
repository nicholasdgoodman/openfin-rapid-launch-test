using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Threading;

using RapidLaunch.Common;

namespace RapidLaunch.AppLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Process> spawnedProcesses = new List<Process>();

        public MainWindow()
        {
            InitializeComponent();

            OpenFinGlobals.RuntimeInstance.Connected += OpenFinRuntime_Connected;
            OpenFinGlobals.RuntimeInstance.Disconnected += OpenFinRuntime_Disconnected;

            OpenFinGlobals.RuntimeInstance.Connect(() => 
            {

            });
        }

        private void OpenFinRuntime_Connected(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectionStatusText.Text = "OpenFin Connected";
                SpawnButton.IsEnabled = true;
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

            Task.Run(() => SpawnChildApps(wpfAppCount, embeddedViewCount, delay));
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

            // Testing finds no significant difference in lanching in series vs in parallel
            // Keeping code in-series for simplicity and allow a sequential per-launch delay         
            //var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = 1 };
            //Parallel.ForEach(appsShouldEmbed, parallelOptions, appShouldEmbed =>  

            foreach (var appShouldEmbed in appsShouldEmbed)
            {
                var processTask = appShouldEmbed ?
                    SpawnEmbeddedAppAsync() :
                    SpawnAppAsync();

                // This call blocks (on purpose) to restrict how many spawns happen at once
                var spawnedProcess = processTask.Result;

                Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId:X8} Got Process    ");

                spawnedProcesses.Add(spawnedProcess);
                spawnedProcess.Exited += SpawnedProcess_Exited;

                Task.Delay(delay).Wait();
            };
        }

        private Task<Process> SpawnEmbeddedAppAsync()
        {
            var appUuid = Guid.NewGuid().ToString();

            var tsc = new TaskCompletionSource<Process>();
            var appOptions = new Openfin.Desktop.ApplicationOptions(
                name: appUuid,
                uuid: appUuid,
                url: OpenFinGlobals.DefaultAppUrl)
            {
                NonPersistent = false
            };

            Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId:X8} App Creating    {appUuid}");

            var app = OpenFinGlobals.RuntimeInstance.CreateApplication(appOptions);
            app.Run(async () =>
            {
                tsc.SetResult(await SpawnAppAsync(appUuid));
            });

            return tsc.Task;
        }

        private Task<Process> SpawnAppAsync(string appUuid = "")
        {
            var tsc = new TaskCompletionSource<Process>();
            tsc.SetResult(SpawnApp(appUuid));
            return tsc.Task;
        }

        private Process SpawnApp(string appUuid)
        {
            var process = Process.Start(new ProcessStartInfo()
            {
                FileName = "SpawnedApp.exe",
                Arguments = $"\"{appUuid}\" {OpenFinGlobals.RuntimeInstance.DesktopConnection.Port}",
                UseShellExecute = false
            });

            process.EnableRaisingEvents = true;

            Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId:X8} Process Started {appUuid}");
            return process;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private void ArrangeButton_Click(object sender, RoutedEventArgs e)
        {
            int n = 0;

            foreach(var spawnedProcess in spawnedProcesses.ToArray())
            {
                if(!spawnedProcess.HasExited)
                {
                    var row = n % 5;
                    var col = n / 5;
                    var xf = 280;
                    var yf = 160;

                    MoveWindow(spawnedProcesses[n].MainWindowHandle, xf * col, yf * row, xf, yf, true);

                    n++;
                }
                else
                {
                    spawnedProcesses.Remove(spawnedProcess);
                }
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
                if(!spawnedProcess.HasExited)
                {
                    spawnedProcess.CloseMainWindow();
                }
                spawnedProcesses.Remove(spawnedProcess);
            }
        }

        private void TerminateButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var spawnedProcess in spawnedProcesses.ToArray())
            {
                if(!spawnedProcess.HasExited)
                {
                    spawnedProcess.Kill();
                }
                spawnedProcesses.Remove(spawnedProcess);
            }
        }
    }
}
