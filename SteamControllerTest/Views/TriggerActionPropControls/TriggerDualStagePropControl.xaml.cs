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
using SteamControllerTest.ViewModels.TriggerActionPropViewModels;
using SteamControllerTest.TriggerActions;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.ActionUtil;

namespace SteamControllerTest.Views.TriggerActionPropControls
{
    /// <summary>
    /// Interaction logic for TriggerDualStagePropControl.xaml
    /// </summary>
    public partial class TriggerDualStagePropControl : UserControl
    {
        public class DualStageBindingArgs : EventArgs
        {
            private AxisDirButton pullBtn;
            public AxisDirButton PullBtn => pullBtn;

            public DualStageBindingArgs(AxisDirButton pullBtn)
            {
                this.pullBtn = pullBtn;
            }
        }

        private TriggerDualStagePropViewModel trigDualStagePropVM;

        public event EventHandler<int> ActionTypeIndexChanged;
        public event EventHandler<DualStageBindingArgs> RequestFuncEditor;

        public TriggerDualStagePropControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, TriggerMapAction action)
        {
            trigDualStagePropVM = new TriggerDualStagePropViewModel(mapper, action);

            DataContext = trigDualStagePropVM;

            triggerSelectControl.PostInit(mapper, action);

            triggerSelectControl.TrigActionSelVM.SelectedIndexChanged += TrigActionSelVM_SelectedIndexChanged;
        }

        private void TrigActionSelVM_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActionTypeIndexChanged?.Invoke(this,
                triggerSelectControl.TrigActionSelVM.SelectedIndex);
        }

        private void btnEditOpenTest_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DualStageBindingArgs(trigDualStagePropVM.Action.FullPullActButton));
        }

        private void btnEditOpenSoftTest_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DualStageBindingArgs(trigDualStagePropVM.Action.SoftPullActButton));
        }
    }
}
