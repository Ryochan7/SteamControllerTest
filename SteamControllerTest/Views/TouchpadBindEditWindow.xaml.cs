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
using SteamControllerTest.ActionUtil;
using SteamControllerTest.TouchpadActions;
using SteamControllerTest.Views.TouchpadActionPropControls;

namespace SteamControllerTest.Views
{
    /// <summary>
    /// Interaction logic for TouchpadBindEditWindow.xaml
    /// </summary>
    public partial class TouchpadBindEditWindow : Window
    {
        private TouchpadBindEditViewModel touchBindEditVM;
        public TouchpadBindEditViewModel TouchBindEditVM => touchBindEditVM;

        public event EventHandler<TouchpadMapAction> TouchActionUpdated;

        public TouchpadBindEditWindow()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, TouchpadMapAction action)
        {
            touchBindEditVM = new TouchpadBindEditViewModel(mapper, action);

            DataContext = touchBindEditVM;

            touchpadSelectControl.PostInit(mapper, action);
            touchpadSelectControl.TouchOutputSelVM.SelectedIndexChanged += TouchOutputSelVM_SelectedIndexChanged;

            SetupDisplayControl();
        }

        private void TouchOutputSelVM_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedInd = touchpadSelectControl.TouchOutputSelVM.SelectedIndex;
            TouchpadMapAction tempAction = touchBindEditVM.PrepareNewAction(selectedInd);
            if (tempAction != null)
            {
                tempAction.CopyBaseMapProps(touchBindEditVM.Action);
                touchBindEditVM.MigrateActionId(tempAction);
                touchBindEditVM.SwitchAction(tempAction);
                SetupDisplayControl();
            }
        }

        public void SetupDisplayControl()
        {
            switch(touchBindEditVM.Action)
            {
                case TouchpadStickAction:
                    {
                        TouchpadStickActionPropControl propControl = new TouchpadStickActionPropControl();
                        propControl.PostInit(touchBindEditVM.Mapper, touchBindEditVM.Action);
                        touchBindEditVM.DisplayControl = propControl;
                    }

                    break;
                case TouchpadActionPad:
                    {
                        TouchpadActionPadPropControl propActionPadControl = new TouchpadActionPadPropControl();
                        propActionPadControl.PostInit(touchBindEditVM.Mapper, touchBindEditVM.Action);
                        propActionPadControl.RequestFuncEditor += PropActionPadControl_RequestFuncEditor;
                        touchBindEditVM.DisplayControl = propActionPadControl;
                    }

                    break;
                case TouchpadMouseJoystick:
                    {
                        TouchpadMouseJoystickPropControl propControl = new TouchpadMouseJoystickPropControl();
                        propControl.PostInit(touchBindEditVM.Mapper, touchBindEditVM.Action);
                        touchBindEditVM.DisplayControl = propControl;
                    }

                    break;
                case TouchpadMouse:
                    {
                        TouchpadMousePropControl propControl = new TouchpadMousePropControl();
                        propControl.PostInit(touchBindEditVM.Mapper, touchBindEditVM.Action);
                        touchBindEditVM.DisplayControl = propControl;
                    }

                    break;
                case TouchpadAbsAction:
                    {
                        TouchpadAbsMousePropControl propControl = new TouchpadAbsMousePropControl();
                        propControl.PostInit(touchBindEditVM.Mapper, touchBindEditVM.Action);
                        propControl.RequestFuncEditor += PropControl_RequestFuncEditor;
                        touchBindEditVM.DisplayControl = propControl;
                    }

                    break;
                default:
                    touchBindEditVM.DisplayControl = null;
                    break;
            }
        }

        private void PropControl_RequestFuncEditor(object sender, TouchpadAbsMousePropControl.ButtonBindingArgs e)
        {
            FuncBindingControl tempControl = new FuncBindingControl();
            tempControl.PostInit(touchBindEditVM.Mapper, e.ActionBtn);
            tempControl.RequestBindingEditor += TempControl_RequestBindingEditor;
            UserControl oldControl = touchBindEditVM.DisplayControl;
            touchpadSelectControl.Visibility = Visibility.Collapsed;
            tempControl.RequestClose += (sender, args) =>
            {
                touchBindEditVM.DisplayControl = oldControl;
                touchpadSelectControl.Visibility = Visibility.Visible;
            };

            touchBindEditVM.DisplayControl = tempControl;
        }

        private void PropActionPadControl_RequestFuncEditor(object sender, TouchpadActionPadPropControl.DirButtonBindingArgs e)
        {
            FuncBindingControl tempControl = new FuncBindingControl();
            tempControl.PostInit(touchBindEditVM.Mapper, e.DirBtn);
            tempControl.RequestBindingEditor += TempControl_RequestBindingEditor;
            UserControl oldControl = touchBindEditVM.DisplayControl;
            touchpadSelectControl.Visibility = Visibility.Collapsed;
            tempControl.RequestClose += (sender, args) =>
            {
                touchBindEditVM.DisplayControl = oldControl;
                touchpadSelectControl.Visibility = Visibility.Visible;
            };

            touchBindEditVM.DisplayControl = tempControl;
        }

        private void TempControl_RequestBindingEditor(object sender, ActionFunc e)
        {
            OutputBindingEditorControl tempControl = new OutputBindingEditorControl();
            FuncBindingControl bindControl = sender as FuncBindingControl;
            tempControl.PostInit(touchBindEditVM.Mapper, bindControl.FuncBindVM.Action, e);
            UserControl oldControl = bindControl;
            tempControl.Finished += (sender, args) =>
            {
                bindControl.RefreshView();
                touchBindEditVM.DisplayControl = oldControl;
            };

            touchBindEditVM.DisplayControl = tempControl;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            switch (touchBindEditVM.Action)
            {
                case TouchpadStickAction:
                    {
                        TouchpadStickActionPropControl tempControl = touchBindEditVM.DisplayControl as TouchpadStickActionPropControl;
                        if (tempControl.TouchStickPropVM.Action != touchBindEditVM.Action)
                        {
                            touchBindEditVM.UpdateAction(tempControl.TouchStickPropVM.Action);
                            TouchActionUpdated?.Invoke(this, tempControl.TouchStickPropVM.Action);
                        }
                    }

                    break;
                case TouchpadActionPad:
                    {
                        TouchpadActionPadPropControl tempControl = touchBindEditVM.DisplayControl as TouchpadActionPadPropControl;
                        if (tempControl.TouchActionPropVM.Action != touchBindEditVM.Action)
                        {
                            touchBindEditVM.UpdateAction(tempControl.TouchActionPropVM.Action);
                            TouchActionUpdated?.Invoke(this, tempControl.TouchActionPropVM.Action);
                        }
                    }

                    break;
                case TouchpadMouseJoystick:
                    {
                        TouchpadMouseJoystickPropControl tempControl = touchBindEditVM.DisplayControl as TouchpadMouseJoystickPropControl;
                        if (tempControl.TouchMouseJoyPropVM.Action != touchBindEditVM.Action)
                        {
                            touchBindEditVM.UpdateAction(tempControl.TouchMouseJoyPropVM.Action);
                            TouchActionUpdated?.Invoke(this, tempControl.TouchMouseJoyPropVM.Action);
                        }
                    }

                    break;
                case TouchpadMouse:
                    {
                        TouchpadMousePropControl tempControl = touchBindEditVM.DisplayControl as TouchpadMousePropControl;
                        if (tempControl.TouchMousePropVM.Action != touchBindEditVM.Action)
                        {
                            touchBindEditVM.UpdateAction(tempControl.TouchMousePropVM.Action);
                            TouchActionUpdated?.Invoke(this, tempControl.TouchMousePropVM.Action);
                        }
                    }

                    break;
                case TouchpadAbsAction:
                    {
                        TouchpadAbsMousePropControl tempControl = touchBindEditVM.DisplayControl as TouchpadAbsMousePropControl;
                        if (tempControl.TouchAbsMousePropVM.Action != touchBindEditVM.Action)
                        {
                            touchBindEditVM.UpdateAction(tempControl.TouchAbsMousePropVM.Action);
                            TouchActionUpdated?.Invoke(this, tempControl.TouchAbsMousePropVM.Action);
                        }
                    }

                    break;
                default:
                    break;
            }
        }
    }
}
