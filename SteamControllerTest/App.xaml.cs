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
        private AppGlobalData appGlobal;
        private Timer collectTimer;

        private Thread testThread;
        //private Mapper mapper;
        //public Mapper Mapper { get => mapper; }
        public BackendManager Manager { get => manager; }

        private OSDTest osdTestWindow;

        private bool exitApp;
        private string tempProfilePath = string.Empty;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // No longer require profile path to be passed to program
            /*if (e.Args.Length != 1 && !File.Exists(e.Args[0]))
            {
                Application.Current.Shutdown();
                return;
            }
            */

            /*string tempProfilePath = string.Empty;
            if (e.Args.Length == 1 && File.Exists(e.Args[0]))
            {
                tempProfilePath = e.Args[0];
            }
            */

            ArgumentParser parser = new ArgumentParser();
            parser.Parse(e.Args);
            CheckOptions(parser);

            if (exitApp)
            {
                Current.Shutdown(1);
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

            appGlobal = AppGlobalDataSingleton.Instance;
            appGlobal.FindConfigLocation();
            if (!appGlobal.appSettingsDirFound)
            {
                bool createdSkel = appGlobal.CreateBaseConfigSkeleton();
                if (!createdSkel)
                {
                    MessageBox.Show($"Cannot create config folder structure in {appGlobal.appdatapath}. Exiting",
                        "Test", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown(1);
                    return;
                }
            }

            appGlobal.CheckAndCopyExampleProfiles();
            appGlobal.RefreshBaseDriverInfo();
            appGlobal.StartupLoadAppSettings();
            if (!File.Exists(appGlobal.ControllerConfigsPath))
            {
                appGlobal.CreateControllerDeviceSettingsFile();
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            testThread = new Thread(() =>
            {
                manager = new BackendManager(tempProfilePath, appGlobal);
                manager.RequestOSD += Manager_RequestOSD;
                //manager.Start();
                //mapper = new Mapper();
                //mapper.Start();
            });

            testThread.IsBackground = true;
            testThread.Start();
            testThread.Join();

            MainWindow window = new MainWindow();
            window.PostInit(appGlobal);
            //window.MainWinVM.ProfilePath = tempProfilePath;
            window.ProfilePathChanged += Window_ProfilePathChanged;
            MainWindow = window;

            osdTestWindow = new OSDTest();

            window.Show();

            window.StartCheckProcess();

            collectTimer = new Timer(GarbageTask, null, 30000, 30000);
        }

        private void GarbageTask(object state)
        {
            GC.Collect(0, GCCollectionMode.Forced, false);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exp = e.ExceptionObject as Exception;
            bool canAccessMain = Current.Dispatcher.CheckAccess();
            //Trace.WriteLine($"CRASHED {help}");
            if (e.IsTerminating)
            {
                CleanShutDown();
            }
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

        private void CheckOptions(ArgumentParser parser)
        {
            if (parser.HasErrors)
            {
                exitApp = true;
                return;
            }

            if (!string.IsNullOrEmpty(parser.ProfilePath))
            {
                tempProfilePath = parser.ProfilePath;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            CleanShutDown();
        }

        private void CleanShutDown()
        {
            //if (manager.IsRunning)
            {
                Task tempTask = Task.Run(() =>
                {
                    manager?.PreAppStopDown();
                    manager?.Stop();
                });
                tempTask.Wait();

                manager.ShutDown();
            }

            osdTestWindow.Close();
            osdTestWindow = null;
        }
    }
}
