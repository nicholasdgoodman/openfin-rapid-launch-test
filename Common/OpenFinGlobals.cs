using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fin = Openfin.Desktop;

namespace RapidLaunch.Common
{
    public class OpenFinGlobals
    {
        static OpenFinGlobals()
        {
            var openfinOptions = new Fin.RuntimeOptions()
            {
                Version = "9.*"
            };

            RuntimeInstance = Fin.Runtime.GetRuntimeInstance(openfinOptions);
            //DefaultAppUrl = "http://akveo.com/blur-admin/#/dashboard";
            DefaultAppUrl = "http://google.com";
            //DefaultAppUrl = "http://flatlogic.github.io/angular-material-dashboard/#/dashboard";
        }

        public static Fin.Runtime RuntimeInstance { get; private set; }

        public static string DefaultAppUrl { get; private set; }
    }
}
