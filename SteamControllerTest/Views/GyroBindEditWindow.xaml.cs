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
using System.Windows.Shapes;
using SteamControllerTest.ViewModels;
using SteamControllerTest.Views.GyroActionPropControls;
using SteamControllerTest.GyroActions;

namespace SteamControllerTest.Views
{
    /// <summary>
    /// Interaction logic for GyroBindEditWindow.xaml
    /// </summary>
    public partial class GyroBindEditWindow : Window
    {
        private GyroBindEditViewModel gyroBindEditVM;
        public GyroBindEditViewModel GyroBindEditVM => gyroBindEditVM;

        public GyroBindEditWindow()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, GyroMapAction action)
        {
            gyroBindEditVM = new GyroBindEditViewModel(mapper, action);

            DataContext = gyroBindEditVM;

            SetupDisplayControl();
        }

        public void SetupDisplayControl()
        {
            switch(gyroBindEditVM.Action)
            {
                case GyroNoMapAction:
                    {
                        GyroNoActionControl propControl = new GyroNoActionControl();
                        propControl.PostInit(gyroBindEditVM.Mapper, gyroBindEditVM.Action);
                        propControl.ActionTypeIndexChanged += PropControl_ActionTypeIndexChanged;
                        gyroBindEditVM.DisplayControl = propControl;
                    }

                    break;
                case GyroMouse:
                    {
                        GyroMousePropControl propControl = new GyroMousePropControl();
                        propControl.PostInit(gyroBindEditVM.Mapper, gyroBindEditVM.Action);
                        propControl.ActionTypeIndexChanged += PropControl_ActionTypeIndexChanged;
                        gyroBindEditVM.DisplayControl = propControl;
                    }

                    break;
                case GyroMouseJoystick:
                    {
                        GyroMouseJoystickPropControl propControl = new GyroMouseJoystickPropControl();
                        propControl.PostInit(gyroBindEditVM.Mapper, gyroBindEditVM.Action);
                        propControl.ActionTypeIndexChanged += PropControl_ActionTypeIndexChanged;
                        gyroBindEditVM.DisplayControl = propControl;
                    }

                    break;
                default:
                    gyroBindEditVM.DisplayControl = null;
                    break;
            }
        }

        private void PropControl_ActionTypeIndexChanged(object sender, int ind)
        {
            GyroMapAction tempAction = gyroBindEditVM.PrepareNewAction(ind);
            if (tempAction != null)
            {
                tempAction.CopyBaseMapProps(gyroBindEditVM.Action);
                gyroBindEditVM.MigrateActionId(tempAction);
                gyroBindEditVM.SwitchAction(tempAction);
                SetupDisplayControl();
            }
        }
    }
}
