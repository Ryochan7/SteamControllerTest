using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
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

        private void Application_Startup(object sender, StartupEventArgs e)
        {
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
                manager = new BackendManager();
                //manager.Start();
                //mapper = new Mapper();
                //mapper.Start();
            });

            testThread.IsBackground = true;
            testThread.Start();
            testThread.Join();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            manager.Stop();
        }
    }
}
