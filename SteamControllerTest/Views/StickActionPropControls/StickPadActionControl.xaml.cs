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
using SteamControllerTest.Views;
using SteamControllerTest.ViewModels.StickActionPropViewModels;
using SteamControllerTest.StickActions;

namespace SteamControllerTest.Views.StickActionPropControls
{
    /// <summary>
    /// Interaction logic for StickPadActionControl.xaml
    /// </summary>
    public partial class StickPadActionControl : UserControl
    {
        private StickPadActionPropViewModel stickPadActVM;
        public StickPadActionPropViewModel StickPadActVM => stickPadActVM;

        public event EventHandler<int> ActionTypeIndexChanged;

        public StickPadActionControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, StickMapAction action)
        {
            stickPadActVM = new StickPadActionPropViewModel(mapper, action);
            DataContext = stickPadActVM;

            stickSelectControl.PostInit(mapper, action);
            stickSelectControl.StickActSelVM.SelectedIndexChanged += StickActSelVM_SelectedIndexChanged;
        }

        private void StickActSelVM_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActionTypeIndexChanged?.Invoke(this,
                stickSelectControl.StickActSelVM.SelectedIndex);
        }
    }
}
