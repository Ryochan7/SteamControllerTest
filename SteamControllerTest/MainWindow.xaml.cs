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
using Microsoft.Win32;

using SteamControllerTest.ViewModels;
using SteamControllerTest.SteamControllerLibrary;
using System.Windows.Interop;
using HidLibrary;
using System.Diagnostics;

namespace SteamControllerTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel mainWinVM = new MainWindowViewModel();
        public MainWindowViewModel MainWinVM => mainWinVM;

        private ControllerListViewModel controlListVM;
        public ControllerListViewModel ControlListVM => controlListVM;

        private IntPtr regHandle = new IntPtr();
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int HOTPLUG_CHECK_DELAY = 2000;

        public class ProfilePathEventArgs : EventArgs
        {
            private string filePath;
            public string FilePath
            {
                get { return filePath; }
            }

            public ProfilePathEventArgs(string filePath) : base()
            {
                this.filePath = filePath;
            }
        }

        public event EventHandler<ProfilePathEventArgs> ProfilePathChanged;

        private AppGlobalData appGlobal;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void PostInit(AppGlobalData appGlobal)
        {
            this.appGlobal = appGlobal;

            //mainWinVM.ProfilePathChanged += MainWinVM_ProfilePathChanged;

            DataContext = mainWinVM;

            BackendManager manager = (App.Current as App).Manager;
            controlListVM = new ControllerListViewModel(manager);
            controlListVM.ReadProfileFailure += ControlListVM_ReadProfileFailure;
            deviceListView.DataContext = controlListVM;
        }

        private void ControlListVM_ReadProfileFailure(object sender,
            ReadProfileFailException e)
        {
            // Assume event will be invoked from non-GUI thread
            Dispatcher.BeginInvoke((Action)(() =>
            {
                string windowMessage = $"{e.ExtraMessage}\n\n{e.InnerJsonException.Message}";
                string titleCaption = $"Profile read failed";
                MessageBox.Show(windowMessage, titleCaption,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BackendManager manager = (App.Current as App).Manager;
            if (!manager.IsRunning)
            {
                StartCheckProcess();
            }
            else
            {
                StopCheckProcess();
            }
        }

        public async void StartCheckProcess()
        {
            serviceChangeBtn.IsEnabled = false;
            deviceListView.IsEnabled = false;

            await Task.Run(async () =>
            {
                App thing = Application.Current as App;
                thing.Manager.Start();
                // Keep arbitrary delay for now. Not really needed though
                await Task.Delay(1000);
            });

            mainWinVM.ServiceBtnText = MainWindowViewModel.DEFAULT_STOP_TEXT;
            serviceChangeBtn.IsEnabled = true;
            deviceListView.IsEnabled = true;
        }

        public async void StopCheckProcess()
        {
            serviceChangeBtn.IsEnabled = false;
            deviceListView.IsEnabled = false;

            await Task.Run(async () =>
            {
                App thing = Application.Current as App;
                thing.Manager.Stop();
                // Keep arbitrary delay for now. Not really needed though
                await Task.Delay(1000);
            });

            mainWinVM.ServiceBtnText = MainWindowViewModel.DEFAULT_START_TEXT;
            serviceChangeBtn.IsEnabled = true;
            deviceListView.IsEnabled = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            BackendManager manager = (Application.Current as App).Manager;
            if (!manager.ChangingService)
            {
                Util.UnregisterNotify(regHandle);
                Application.Current.Shutdown(0);
            }
        }

        private void FindProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.InitialDirectory = appGlobal.baseProfilesPath;
            fileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                bool check = mainWinVM.CheckProfilePath(fileDialog.FileName);
                if (check)
                {
                    //startBtn.IsEnabled = true;
                }
            }
        }

        private void MainWinVM_ProfilePathChanged(object sender, EventArgs e)
        {
            ProfilePathChanged?.Invoke(this,
                new ProfilePathEventArgs(mainWinVM.ProfilePath));
        }

        private void DeviceListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (controlListVM.SelectedIndex >= 0)
            {
                int selectedIndex = controlListVM.SelectedIndex;
                SteamControllerDevice device = controlListVM.ControllerList[selectedIndex].Device;
                ControllerConfigWin dialog = new ControllerConfigWin();
                dialog.PostInit(device);
                dialog.ShowDialog();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            HookWindowMessages(source);
            source.AddHook(WndProc);
        }

        private void HookWindowMessages(HwndSource source)
        {
            Guid hidGuid = new Guid();
            NativeMethods.HidD_GetHidGuid(ref hidGuid);
            bool result = Util.RegisterNotify(source.Handle, hidGuid, ref regHandle);
            if (!result)
            {
                App.Current.Shutdown();
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
            IntPtr lParam, ref bool handled)
        {
            switch(msg)
            {
                case Util.WM_DEVICECHANGE:
                    {
                        App current = Application.Current as App;
                        BackendManager manager = current.Manager;
                        if (!manager.IsRunning)
                        {
                            break;
                        }

                        Int32 Type = wParam.ToInt32();
                        if (Type == DBT_DEVICEARRIVAL ||
                            Type == DBT_DEVICEREMOVECOMPLETE)
                        {
                            Trace.WriteLine($"IN THIS {System.Threading.Thread.CurrentThread.ManagedThreadId.ToString()}");
                            Task.Run(() =>
                            {
                                InnerHotplug(manager);
                            });
                        }
                    }

                    break;
                default:
                    break;
            }

            return IntPtr.Zero;
        }

        private void InnerHotplug(BackendManager manager)
        {
            /*Trace.WriteLine("ENTERING INNER HOTPLUG");
            Task.Delay(5000).Wait();
            Trace.WriteLine("EXITING INNER HOTPLUG");
            */
            System.Threading.Thread.Sleep(HOTPLUG_CHECK_DELAY);
            manager.EventDispatcher.BeginInvoke((Action)(() =>
            {
                manager.Hotplug();
            }));
        }

        private void EditPro_Click(object sender, RoutedEventArgs e)
        {
            if (controlListVM.SelectedIndex >= 0)
            {
                int selectedIndex = controlListVM.SelectedIndex;
                Mapper mapper = (App.Current as App).Manager.MapperDict[selectedIndex];
                DeviceListItem item = controlListVM.ControllerList[selectedIndex];
                ProfileEntity profileEnt = controlListVM.DeviceProfileList.ProfileListCol[item.ProfileIndex];
                //string profilePath = controlListVM.DeviceProfileList.ProfileListCol[item.ProfileIndex].ProfilePath;
                //SteamControllerDevice device = controlListVM.ControllerList[selectedIndex].Device;

                ProfileEditorTest profileWin = new ProfileEditorTest();
                profileWin.PostInit(mapper, profileEnt, mapper.ActionProfile);
                profileWin.ShowDialog();
            }
        }
    }
}
