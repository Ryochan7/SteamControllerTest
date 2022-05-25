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
using SteamControllerTest.Views.StickActionPropControls;
using SteamControllerTest.ViewModels;
using SteamControllerTest.StickActions;

namespace SteamControllerTest.Views
{
    /// <summary>
    /// Interaction logic for StickBindEditWindow.xaml
    /// </summary>
    public partial class StickBindEditWindow : Window
    {
        private StickBindEditViewModel stickBindEditVM;
        public StickBindEditViewModel StickBindEditVM => stickBindEditVM;

        public StickBindEditWindow()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, StickMapAction action)
        {
            stickBindEditVM = new StickBindEditViewModel(mapper, action);

            DataContext = stickBindEditVM;

            SetupDisplayControl();
        }

        public void SetupDisplayControl()
        {
            switch(stickBindEditVM.Action)
            {
                case StickNoAction:
                    {
                        StickNoActPropControl propControl = new StickNoActPropControl();
                        propControl.PostInit(stickBindEditVM.Mapper, stickBindEditVM.Action);
                        propControl.ActionTypeIndexChanged += PropControl_ActionTypeIndexChanged; ;
                        stickBindEditVM.DisplayControl = propControl;
                    }

                    break;
                case StickTranslate:
                    break;
                case StickPadAction:
                    break;
                default:
                    break;
            }
        }

        private void PropControl_ActionTypeIndexChanged(object sender, int ind)
        {
            StickMapAction tempAction = stickBindEditVM.PrepareNewAction(ind);
            if (tempAction != null)
            {
                tempAction.CopyBaseMapProps(stickBindEditVM.Action);
                stickBindEditVM.MigrateActionId(tempAction);
                stickBindEditVM.SwitchAction(tempAction);
                SetupDisplayControl();
            }
        }
    }
}
