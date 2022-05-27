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
using SteamControllerTest.ButtonActions;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.TouchpadActions;
using SteamControllerTest.ViewModels.TouchpadActionPropViewModels;

namespace SteamControllerTest.Views.TouchpadActionPropControls
{
    /// <summary>
    /// Interaction logic for TouchpadActionPadPropControl.xaml
    /// </summary>
    public partial class TouchpadActionPadPropControl : UserControl
    {
        public class DirButtonBindingArgs : EventArgs
        {
            private ButtonAction dirBtn;
            public ButtonAction DirBtn => dirBtn;

            public DirButtonBindingArgs(ButtonAction dirBtn)
            {
                this.dirBtn = dirBtn;
            }
        }

        private TouchpadActionPadPropViewModel touchActionPropVM;
        public TouchpadActionPadPropViewModel TouchActionPropVM => touchActionPropVM;

        public event EventHandler<DirButtonBindingArgs> RequestFuncEditor;

        public TouchpadActionPadPropControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, TouchpadMapAction action)
        {
            touchActionPropVM = new TouchpadActionPadPropViewModel(mapper, action);

            DataContext = touchActionPropVM;
        }

        private void btnUpEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Up]));
        }

        private void btnDownEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Down]));
        }

        private void btnLeftEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Left]));
        }

        private void btnRightEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Right]));
        }

        private void btnUpLeftEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.UpLeft]));
        }

        private void btnUpRightEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.UpRight]));
        }

        private void btnDownLeftEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.DownLeft]));
        }

        private void btnDownRightEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.DownRight]));
        }
    }
}
