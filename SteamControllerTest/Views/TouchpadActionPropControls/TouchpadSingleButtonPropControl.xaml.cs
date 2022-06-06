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
using static SteamControllerTest.Views.TouchpadActionPropControls.TouchpadActionPadPropControl;

namespace SteamControllerTest.Views.TouchpadActionPropControls
{
    /// <summary>
    /// Interaction logic for TouchpadSingleButtonPropControl.xaml
    /// </summary>
    public partial class TouchpadSingleButtonPropControl : UserControl
    {
        private TouchpadSingleButtonPropViewModel touchSingleBtnVM;
        public TouchpadSingleButtonPropViewModel TouchSingleBtnVM => touchSingleBtnVM;

        public event EventHandler<DirButtonBindingArgs> RequestFuncEditor;

        public TouchpadSingleButtonPropControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, TouchpadMapAction action)
        {
            touchSingleBtnVM = new TouchpadSingleButtonPropViewModel(mapper, action);
            DataContext = touchSingleBtnVM;
        }

        public void RefreshView()
        {
            // Force re-eval of bindings
            DataContext = null;
            DataContext = touchSingleBtnVM;
        }

        private void BtnEditBinding_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchSingleBtnVM.Action.EventButton));
        }
    }
}
