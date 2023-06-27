using SteamControllerTest.TouchpadActions;
using SteamControllerTest.ViewModels.TouchpadActionPropViewModels;
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
using SteamControllerTest.StickActions;
using SteamControllerTest.ViewModels.StickActionPropViewModels;
using static SteamControllerTest.Views.TouchpadActionPropControls.TouchpadActionPadPropControl;

namespace SteamControllerTest.Views.StickActionPropControls
{
    /// <summary>
    /// Interaction logic for StickCircularPropControl.xaml
    /// </summary>
    public partial class StickCircularPropControl : UserControl
    {
        private StickCircularPropViewModel stickCircVM;
        public StickCircularPropViewModel StickCircVM => stickCircVM;

        public event EventHandler<DirButtonBindingArgs> RequestFuncEditor;
        public event EventHandler<int> ActionTypeIndexChanged;

        public StickCircularPropControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, StickMapAction action)
        {
            stickCircVM = new StickCircularPropViewModel(mapper, action);
            DataContext = stickCircVM;

            stickSelectControl.PostInit(mapper, action);
            stickSelectControl.StickActSelVM.SelectedIndexChanged += StickActSelVM_SelectedIndexChanged;
        }

        public void RefreshView()
        {
            // Force re-eval of bindings
            DataContext = null;
            DataContext = stickCircVM;
        }

        private void BtnEditForward_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(stickCircVM.Action.ClockWiseBtn,
                !stickCircVM.Action.UseParentCircButtons[0],
                stickCircVM.UpdateClockWiseBtn));
        }

        private void BtnEditBackward_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(stickCircVM.Action.CounterClockwiseBtn,
                !stickCircVM.Action.UseParentCircButtons[1],
                stickCircVM.UpdateCounterClockWiseBtn));
        }

        private void StickActSelVM_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActionTypeIndexChanged?.Invoke(this,
                stickSelectControl.StickActSelVM.SelectedIndex);
        }
    }
}
