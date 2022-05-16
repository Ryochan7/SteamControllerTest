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
using SteamControllerTest.ActionUtil;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.ViewModels;

namespace SteamControllerTest.Views
{
    /// <summary>
    /// Interaction logic for OutputBindingEditorControl.xaml
    /// </summary>
    public partial class OutputBindingEditorControl : UserControl
    {
        private ButtonActionEditViewModel buttonActionEditVM;
        public event EventHandler Finished;

        public OutputBindingEditorControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, ButtonAction currentAction, ActionFunc func)
        {
            buttonActionEditVM = new ButtonActionEditViewModel(mapper, currentAction, func);

            DataContext = buttonActionEditVM;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Finished?.Invoke(this, EventArgs.Empty);
        }

        private void AddOutputSlot_Click(object sender, RoutedEventArgs e)
        {
            buttonActionEditVM.AddTempOutputSlot();
        }

        private void RemoveOutputSlot_Click(object sender, RoutedEventArgs e)
        {
            DataContext = null;

            buttonActionEditVM.RemoveOutputSlot(buttonActionEditVM.SelectedSlotItemIndex);

            DataContext = buttonActionEditVM;
        }
    }
}
