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
using SteamControllerTest.Views.TriggerActionPropControls;
using SteamControllerTest.TriggerActions;

namespace SteamControllerTest.Views
{
    /// <summary>
    /// Interaction logic for TriggerBindEditWindow.xaml
    /// </summary>
    public partial class TriggerBindEditWindow : Window
    {
        private TriggerBindEditViewModel trigBindEditVM;
        public TriggerBindEditViewModel TrigBindEditVM => trigBindEditVM;

        public TriggerBindEditWindow()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, TriggerMapAction action)
        {
            trigBindEditVM = new TriggerBindEditViewModel(mapper, action);

            DataContext = trigBindEditVM;

            SetupDisplayControl();
        }

        public void SetupDisplayControl()
        {
            switch (trigBindEditVM.Action)
            {
                case TriggerTranslate:
                    {
                        TriggerTranslatePropControl propControl = new TriggerTranslatePropControl();
                        propControl.PostInit(trigBindEditVM.Mapper, trigBindEditVM.Action);
                        propControl.ActionTypeIndexChanged += PropControl_ActionTypeIndexChanged;
                        trigBindEditVM.DisplayControl = propControl;
                    }

                    break;
                case TriggerDualStageAction:
                    {
                        TriggerDualStagePropControl propControl = new TriggerDualStagePropControl();
                        propControl.PostInit(trigBindEditVM.Mapper, trigBindEditVM.Action);
                        propControl.ActionTypeIndexChanged += PropControl_ActionTypeIndexChanged;
                        trigBindEditVM.DisplayControl = propControl;
                    }

                    break;
                case TriggerNoAction:
                    {
                        TriggerNoActPropControl propControl = new TriggerNoActPropControl();
                        propControl.PostInit(trigBindEditVM.Mapper, trigBindEditVM.Action);
                        propControl.ActionTypeIndexChanged += PropControl_ActionTypeIndexChanged;
                        trigBindEditVM.DisplayControl = propControl;
                    }

                    break;

                default:
                    trigBindEditVM.DisplayControl = null;
                    break;
            }
        }

        private void PropControl_ActionTypeIndexChanged(object sender, int ind)
        {
            TriggerMapAction tempAction = trigBindEditVM.PrepareNewAction(ind);
            if (tempAction != null)
            {
                tempAction.CopyBaseMapProps(trigBindEditVM.Action);
                trigBindEditVM.MigrateActionId(tempAction);
                trigBindEditVM.SwitchAction(tempAction);
                SetupDisplayControl();
            }
        }
    }
}
