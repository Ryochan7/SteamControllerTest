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
using SteamControllerTest.ButtonActions;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.TouchpadActions;
using SteamControllerTest.ViewModels.TouchpadActionPropViewModels;

namespace SteamControllerTest.Views.TouchpadActionPropControls
{
    /// <summary>
    /// Interaction logic for TouchpadActionPadPropControl.xaml
    /// </summary>
    public partial class TouchpadActionPadPropControl : UserControl
    {
        public class DirButtonBindingArgs : EventArgs
        {
            private ButtonAction dirBtn;
            public ButtonAction DirBtn => dirBtn;

            private bool realAction = false;
            public bool RealAction => realAction;

            public delegate void UpdateActionHandler(ButtonAction oldAction, ButtonAction newAction);
            private UpdateActionHandler updateActHandler;
            public UpdateActionHandler UpdateActHandler => updateActHandler;

            public DirButtonBindingArgs(ButtonAction dirBtn, bool realAction = false, UpdateActionHandler updateActDel = null)
            {
                this.dirBtn = dirBtn;
                this.realAction = realAction;
                this.updateActHandler = updateActDel;
            }
        }

        private TouchpadActionPadPropViewModel touchActionPropVM;
        public TouchpadActionPadPropViewModel TouchActionPropVM => touchActionPropVM;

        public event EventHandler<DirButtonBindingArgs> RequestFuncEditor;

        public TouchpadActionPadPropControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, TouchpadMapAction action)
        {
            touchActionPropVM = new TouchpadActionPadPropViewModel(mapper, action);

            DataContext = touchActionPropVM;
        }

        public void RefreshView()
        {
            // Force re-eval of bindings
            DataContext = null;
            DataContext = touchActionPropVM;
        }

        private void btnUpEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Up],
                touchActionPropVM.Action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_UP),
                touchActionPropVM.UpdateUpDirAction));
        }

        private void btnDownEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Down],
                touchActionPropVM.Action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_DOWN),
                touchActionPropVM.UpdateDownDirAction));
        }

        private void btnLeftEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Left],
                touchActionPropVM.Action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_LEFT),
                touchActionPropVM.UpdateLeftDirAction));
        }

        private void btnRightEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Right],
                touchActionPropVM.Action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_RIGHT),
                touchActionPropVM.UpdateRightAction));
        }

        private void btnUpLeftEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.UpLeft],
                touchActionPropVM.Action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_UPLEFT),
                touchActionPropVM.UpdateUpLeftAction));
        }

        private void btnUpRightEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.UpRight],
                touchActionPropVM.Action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_UPRIGHT),
                touchActionPropVM.UpdateUpRightAction));
        }

        private void btnDownLeftEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.DownLeft],
                touchActionPropVM.Action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_DOWNLEFT),
                touchActionPropVM.UpdateDownLeftAction));
        }

        private void btnDownRightEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(touchActionPropVM.Action.EventCodes4[(int)TouchpadActionPad.DpadDirections.DownRight],
                touchActionPropVM.Action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_DOWNRIGHT),
                touchActionPropVM.UpdateDownRightAction));
        }
    }
}
