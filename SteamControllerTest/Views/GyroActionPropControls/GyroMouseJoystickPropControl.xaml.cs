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
using SteamControllerTest.GyroActions;

namespace SteamControllerTest.Views.GyroActionPropControls
{
    /// <summary>
    /// Interaction logic for GyroMouseJoystickPropControl.xaml
    /// </summary>
    public partial class GyroMouseJoystickPropControl : UserControl
    {

        public event EventHandler<int> ActionTypeIndexChanged;

        public GyroMouseJoystickPropControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, GyroMapAction action)
        {
            gyroSelectControl.PostInit(mapper, action);
            gyroSelectControl.GyroActSelVM.SelectedIndexChanged += GyroActSelVM_SelectedIndexChanged;
        }

        private void GyroActSelVM_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActionTypeIndexChanged?.Invoke(this,
                gyroSelectControl.GyroActSelVM.SelectedIndex);
        }
    }
}
