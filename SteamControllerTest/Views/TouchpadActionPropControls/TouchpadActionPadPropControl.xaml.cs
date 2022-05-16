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
        private TouchpadActionPadPropViewModel touchActionPropVM;

        public TouchpadActionPadPropControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, TouchpadMapAction action)
        {
            touchActionPropVM = new TouchpadActionPadPropViewModel(mapper, action);

            DataContext = touchActionPropVM;
        }
    }
}
