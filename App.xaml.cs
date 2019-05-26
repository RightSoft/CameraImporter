using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CameraImporter.SystemSpecific.Genetec;

namespace CameraImporter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            SdkAssemblyLoader.Stop();
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            SdkAssemblyLoader.Start();
            base.OnStartup(e);
        }
    }
}
