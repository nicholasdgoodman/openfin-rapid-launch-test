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

using RapidLaunch.Common;

namespace RapidLaunch.SpawnedApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            OpenFinGlobals.RuntimeInstance.Connected += OpenFinRuntime_Connected;
            OpenFinGlobals.RuntimeInstance.Disconnected += OpenFinRuntime_Disconnected;

            OpenFinGlobals.RuntimeInstance.Connect(() =>
            {
                if (!string.IsNullOrEmpty(App.OpenFinUuid))
                {
                    var appToEmbed = OpenFinGlobals.RuntimeInstance.WrapApplication(App.OpenFinUuid);
                    WebContents.Initialize(OpenFinGlobals.RuntimeInstance.Options, appToEmbed.getWindow());
                }
            });

            Task.Run(new Action(PingAppLauncherLoop));
        }

        private void OpenFinRuntime_Connected(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectionStatusText.Text = "OpenFin Connected";
                MainPanel.Background = Brushes.LightGreen;
            });
        }

        private void OpenFinRuntime_Disconnected(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (ConnectionStatusText.Text == "OpenFin Connected")
                {
                    ConnectionStatusText.Text = "OpenFin Disconnected";
                    MainPanel.Background = Brushes.Crimson;
                }
                else
                {
                    ConnectionStatusText.Text = "OpenFin Connect Failure";
                    MainPanel.Background = Brushes.SandyBrown;
                }
            });

        }

        private void PingAppLauncherLoop()
        {
            while(true)
            {
                MessagePublisher.PingAppLauncher();
                Task.Delay(1000).Wait();
            }
        }
    }
}
