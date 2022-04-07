using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using WpfScreenHelper;
using SteamControllerTest.ViewModels;
using NonFormTimer = System.Timers.Timer;

namespace SteamControllerTest
{
    /// <summary>
    /// Interaction logic for OSDTest.xaml
    /// </summary>
    public partial class OSDTest : Window
    {
        private const long WAIT_TIME = 1250;

        private OSDTestViewModel osdViewModel = new OSDTestViewModel();
        private NonFormTimer testTimer = new NonFormTimer();
        private Stopwatch elapsedWatch = new Stopwatch();

        public OSDTest()
        {
            InitializeComponent();

            Focusable = false;
            //Visibility = Visibility.Collapsed;
            this.Hide();

            DataContext = osdViewModel;
            CalculateOverlayWinSize();

            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged; ;

            testTimer.Elapsed += TestTimer_Elapsed;
            testTimer.Interval = 500;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            CalculateOverlayWinSize();
        }

        private void CalculateOverlayWinSize()
        {
            Screen tempScreen = Screen.PrimaryScreen;
            if (tempScreen != null)
            {
                //Trace.WriteLine($"{tempScreen.Bounds.Width}x{tempScreen.Bounds.Height}");

                if (Width > tempScreen.Bounds.Width)
                {
                    Width = Math.Max(MinWidth, Math.Min(tempScreen.Bounds.Width - (tempScreen.Bounds.Width * 0.2), tempScreen.Bounds.Width));
                }

                if (Height > tempScreen.Bounds.Height)
                {
                    Height = Math.Max(MinHeight, Math.Min(tempScreen.Bounds.Height - (tempScreen.Bounds.Height * 0.2), tempScreen.Bounds.Height));
                }

                Top = (tempScreen.Bounds.Height / 2.0) - (Height / 2.0);
                Left = (tempScreen.Bounds.Width / 2.0) - (Width / 2.0);
            }
        }

        private void TestTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            testTimer.Stop();

            if (elapsedWatch.ElapsedMilliseconds <= WAIT_TIME)
            {
                osdViewModel.RefreshTime();
                testTimer.Start();
            }
            else
            {
                elapsedWatch.Stop();
                // Need to invoke in UI thread
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    Hide();
                }));
            }
        }

        //private void Window_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Escape)
        //    {
        //        Application.Current.Shutdown(0);
        //    }
        //}

        //private void Window_Closed(object sender, EventArgs e)
        //{
        //    //Application.Current.Shutdown(0);
        //}

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    // Invoke app shutdown when button is clicked
        //    Application.Current.Shutdown(0);
        //}

        public void DisplayOSD(string message)
        {
            osdViewModel.LayerMessage = message;
            osdViewModel.RefreshTime();

            this.Hide();
            //await Task.Delay(100);
            //this.Activate();
            this.Show();
            //this.Activate();

            elapsedWatch.Restart();
            testTimer.Start();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Set the window style to noactivate.
            var helper = new WindowInteropHelper(this);
            // WS_EX_NOACTIVATE allows mouse clicks within the window boundaries without activating the window
            // https://stackoverflow.com/questions/12591896/disable-wpf-window-focus
            // WS_EX_TRANSPARENT allows mouse clicks to fall through to the window below
            // https://stackoverflow.com/questions/58824122/how-can-i-make-an-overlay-window-that-allows-mouse-clicks-to-pass-through-to-w
            Util.SetWindowLong(helper.Handle, Util.GWL_EXSTYLE,
                Util.GetWindowLong(helper.Handle, Util.GWL_EXSTYLE) | Util.WS_EX_NOACTIVATE | Util.WS_EX_TRANSPARENT);
        }
    }
}
