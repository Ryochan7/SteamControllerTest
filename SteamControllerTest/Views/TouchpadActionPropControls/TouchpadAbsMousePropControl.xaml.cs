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
using SteamControllerTest.ViewModels.TouchpadActionPropViewModels;
using SteamControllerTest.TouchpadActions;
using SteamControllerTest.ButtonActions;

namespace SteamControllerTest.Views.TouchpadActionPropControls
{
    /// <summary>
    /// Interaction logic for TouchpadAbsMousePropControl.xaml
    /// </summary>
    public partial class TouchpadAbsMousePropControl : UserControl
    {
        public class ButtonBindingArgs : EventArgs
        {
            private AxisDirButton actionBtn;
            public AxisDirButton ActionBtn => actionBtn;

            public ButtonBindingArgs(AxisDirButton pullBtn)
            {
                this.actionBtn = pullBtn;
            }
        }

        private TouchpadAbsMousePropViewModel touchAbsMousePropVM;
        public TouchpadAbsMousePropViewModel TouchAbsMousePropVM => touchAbsMousePropVM;

        public event EventHandler<ButtonBindingArgs> RequestFuncEditor;

        public TouchpadAbsMousePropControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, TouchpadMapAction action)
        {
            touchAbsMousePropVM = new TouchpadAbsMousePropViewModel(mapper, action);

            DataContext = touchAbsMousePropVM;
        }

        private void btnEditTest_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new ButtonBindingArgs(touchAbsMousePropVM.Action.RingButton));
        }
    }
}
