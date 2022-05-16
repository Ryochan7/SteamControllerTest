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
using SteamControllerTest.ViewModels;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.ButtonActions;

namespace SteamControllerTest.Views
{
    /// <summary>
    /// Interaction logic for ButtonActionFuncSelectControl.xaml
    /// </summary>
    public partial class ButtonActionFuncSelectControl : UserControl
    {
        private ButtonActionFuncSelectViewModel funcTypeSelectVM;
        public ButtonActionFuncSelectViewModel FuncTypeSelectVM => funcTypeSelectVM;

        public ButtonActionFuncSelectControl()
        {
            InitializeComponent();
        }

        public void PostInit(ActionFunc func)
        {
            funcTypeSelectVM = new ButtonActionFuncSelectViewModel(func);

            DataContext = funcTypeSelectVM;
        }
    }
}
