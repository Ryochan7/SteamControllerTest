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
            if (e.Args.Length != 1 && !File.Exists(e.Args[0]))
            {
                Application.Current.Shutdown();
                return;
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
                manager = new BackendManager(e.Args[0]);
                manager.RequestOSD += Manager_RequestOSD;
                //manager.Start();
                //mapper = new Mapper();
                //mapper.Start();
            });

            testThread.IsBackground = true;
            testThread.Start();
            testThread.Join();

            MainWindow window = new MainWindow();
            MainWindow = window;
            osdTestWindow = new OSDTest();

            window.Show();
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
