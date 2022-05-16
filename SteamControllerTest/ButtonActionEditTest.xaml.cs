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
using SteamControllerTest.ActionUtil;
using SteamControllerTest.ViewModels;
using SteamControllerTest.ButtonActions;

namespace SteamControllerTest
{
    /// <summary>
    /// Interaction logic for ButtonActionEditTest.xaml
    /// </summary>
    public partial class ButtonActionEditTest : Window
    {
        private ButtonActionEditViewModel buttonActionEditVM;

        public ButtonActionEditTest()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, ButtonAction currentAction, ActionFunc func)
        {
            buttonActionEditVM = new ButtonActionEditViewModel(mapper, currentAction, func);

            DataContext = buttonActionEditVM;
        }
    }
}
