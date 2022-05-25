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
                        propControl.RequestFuncEditor += PropControl_RequestFuncEditor;
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

        private void PropControl_RequestFuncEditor(object sender,
            TriggerDualStagePropControl.DualStageBindingArgs e)
        {
            FuncBindingControl tempControl = new FuncBindingControl();
            tempControl.PostInit(trigBindEditVM.Mapper, e.PullBtn);
            tempControl.RequestBindingEditor += TempControl_RequestBindingEditor;
            UserControl oldControl = trigBindEditVM.DisplayControl;
            tempControl.RequestClose += (sender, args) =>
            {
                trigBindEditVM.DisplayControl = oldControl;
            };

            trigBindEditVM.DisplayControl = tempControl;
        }

        private void TempControl_RequestBindingEditor(object sender, ActionUtil.ActionFunc e)
        {
            OutputBindingEditorControl tempControl = new OutputBindingEditorControl();
            FuncBindingControl bindControl = sender as FuncBindingControl;
            tempControl.PostInit(trigBindEditVM.Mapper, bindControl.FuncBindVM.Action, e);
            UserControl oldControl = bindControl;
            tempControl.Finished += (sender, args) =>
            {
                bindControl.RefreshView();
                trigBindEditVM.DisplayControl = oldControl;
                //FuncBindingControl tempControl = new FuncBindingControl();
                //tempControl.PostInit(btnFuncEditVM.Mapper, btnFuncEditVM.Action);
                //tempControl.RequestBindingEditor += TempControl_RequestBindingEditor;
                //btnFuncEditVM.DisplayControl = tempControl;
            };

            trigBindEditVM.DisplayControl = tempControl;
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
