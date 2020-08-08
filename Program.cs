using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Serilog;

namespace RimworldModUpdater
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (File.Exists("log.txt"))
                File.Delete("log.txt");

            Log.Logger = new LoggerConfiguration().WriteTo.File("log.txt").CreateLogger();

            Log.Information("Loading cef");

            Cef.EnableHighDPISupport();
            var settings = new CefSettings();

            settings.DisableGpuAcceleration();

            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            CefSharpSettings.LegacyJavascriptBindingEnabled = true;

            Application.ApplicationExit += delegate(object sender, EventArgs args)
            {
                foreach (var process in Process.GetProcessesByName("steamcmd"))
                {
                    process.Kill();
                }
            };

            // Cleanup steamapps folder.
            if (Directory.Exists("steamcmd/steamapps"))
            {
                try
                {
                    Directory.Delete("steamcmd/steamapps", true);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to cleanup steamapps");
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UpdaterForm());
        }
    }
}
