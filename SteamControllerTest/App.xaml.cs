using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace SteamControllerTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private BackendManager manager;

        private Thread testThread;
        //private Mapper mapper;
        //public Mapper Mapper { get => mapper; }
        public BackendManager Manager { get => manager; }

        private OSDTest osdTestWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // No longer require profile path to be passed to program
            /*if (e.Args.Length != 1 && !File.Exists(e.Args[0]))
            {
                Application.Current.Shutdown();
                return;
            }
            */

            string tempProfilePath = string.Empty;
            if (e.Args.Length == 1 && File.Exists(e.Args[0]))
            {
                tempProfilePath = e.Args[0];
            }

            try
            {
                Process.GetCurrentProcess().PriorityClass =
                    ProcessPriorityClass.High;
            }
            catch { } // Ignore problems raising the priority.

            // Force Normal IO Priority
            IntPtr ioPrio = new IntPtr(2);
            Util.NtSetInformationProcess(Process.GetCurrentProcess().Handle,
                Util.PROCESS_INFORMATION_CLASS.ProcessIoPriority, ref ioPrio, 4);

            // Force Normal Page Priority
            IntPtr pagePrio = new IntPtr(5);
            Util.NtSetInformationProcess(Process.GetCurrentProcess().Handle,
                Util.PROCESS_INFORMATION_CLASS.ProcessPagePriority, ref pagePrio, 4);

            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            testThread = new Thread(() =>
            {
                manager = new BackendManager(tempProfilePath);
                manager.RequestOSD += Manager_RequestOSD;
                //manager.Start();
                //mapper = new Mapper();
                //mapper.Start();
            });

            testThread.IsBackground = true;
            testThread.Start();
            testThread.Join();

            MainWindow window = new MainWindow();
            window.MainWinVM.ProfilePath = tempProfilePath;
            window.ProfilePathChanged += Window_ProfilePathChanged;
            MainWindow = window;

            osdTestWindow = new OSDTest();

            window.Show();
        }

        private void Window_ProfilePathChanged(object sender,
            MainWindow.ProfilePathEventArgs e)
        {
            manager.ProfileFile = e.FilePath;
        }

        private void Manager_RequestOSD(object sender, Mapper.RequestOSDArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Trace.WriteLine("Attempt to display OSD");
                osdTestWindow.DisplayOSD(e.Message);
            });
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            manager?.Stop();

            osdTestWindow.Close();
            osdTestWindow = null;
        }
    }
}
