using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Fin = Openfin.Desktop;

namespace RapidLaunch.SpawnedApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string OpenFinUuid { get; private set; }
        public static int OpenFinPort { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            if(e.Args.Length > 0)
            {
                OpenFinUuid = e.Args[0];
            }

            if(e.Args.Length > 1)
            {
                OpenFinPort = int.Parse(e.Args[1]);
            }

            base.OnStartup(e);
        }
    }
}
