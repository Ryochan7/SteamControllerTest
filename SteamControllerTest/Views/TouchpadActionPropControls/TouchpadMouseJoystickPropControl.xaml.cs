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
using SteamControllerTest.ViewModels.TouchpadActionPropViewModels;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.TouchpadActions;

namespace SteamControllerTest.Views.TouchpadActionPropControls
{
    /// <summary>
    /// Interaction logic for TouchpadMouseJoystickPropControl.xaml
    /// </summary>
    public partial class TouchpadMouseJoystickPropControl : UserControl
    {
        private TouchpadMouseJoystickPropViewModel  touchMouseJoyPropVM;
        private Mapper mapper;
        private TouchpadMapAction action;

        public TouchpadMouseJoystickPropControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, TouchpadMapAction action)
        {
            touchMouseJoyPropVM = new TouchpadMouseJoystickPropViewModel(mapper, action);

            DataContext = touchMouseJoyPropVM;
        }
    }
}
