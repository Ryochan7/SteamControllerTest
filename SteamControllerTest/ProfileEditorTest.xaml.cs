using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using SteamControllerTest.ButtonActions;
using SteamControllerTest.Views;
using SteamControllerTest.ViewModels;
using SteamControllerTest.TouchpadActions;

namespace SteamControllerTest
{
    /// <summary>
    /// Interaction logic for ProfileEditorTest.xaml
    /// </summary>
    public partial class ProfileEditorTest : Window
    {
        private ProfileEditorTestViewModel editorTestVM;

        public ProfileEditorTest()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, ProfileEntity profileEnt, Profile currentProfile)
        {
            editorTestVM = new ProfileEditorTestViewModel(mapper, profileEnt, currentProfile);

            DataContext = editorTestVM;

            editorTestVM.Test();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string mapTag = (sender as Button).Tag.ToString();
            int ind = editorTestVM.ButtonBindingsIndexDict[mapTag];
            Debug.WriteLine(mapTag);
            
            ButtonMapAction tempAction = editorTestVM.ButtonBindings[ind].MappedAction;
            if (tempAction.GetType() == typeof(ButtonAction))
            {
                Trace.WriteLine($"TYPE {tempAction.GetType()}");

                ButtonFuncEditWindow btnFuncEditWin = new ButtonFuncEditWindow();
                btnFuncEditWin.PostInit(editorTestVM.DeviceMapper, tempAction);
                btnFuncEditWin.ShowDialog();

                editorTestVM.ButtonBindings[ind].UpdateAction(btnFuncEditWin.BtnFuncEditVM.Action);

                //ButtonActionEditTest btnTestEdit = new ButtonActionEditTest();
                //btnTestEdit.PostInit(tempAction as ButtonAction, editorTestVM.DeviceMapper);
                //btnTestEdit.ShowDialog();
            }
            else if (tempAction.GetType() == typeof(ButtonNoAction))
            {
                ButtonFuncEditWindow btnFuncEditWin = new ButtonFuncEditWindow();
                btnFuncEditWin.PostInit(editorTestVM.DeviceMapper, tempAction);
                btnFuncEditWin.ShowDialog();

                editorTestVM.ButtonBindings[ind].UpdateAction(btnFuncEditWin.BtnFuncEditVM.Action);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            int selectedInd = editorTestVM.SelectTouchBindIndex;
            if (selectedInd >= 0)
            {
                TouchpadMapAction tempAction = editorTestVM.TouchpadBindings[selectedInd].MappedAction;
                TouchpadBindEditWindow touchBindEditWin = new TouchpadBindEditWindow();
                touchBindEditWin.PostInit(editorTestVM.DeviceMapper, tempAction);
                touchBindEditWin.ShowDialog();

                editorTestVM.TouchpadBindings[selectedInd].UpdateAction(touchBindEditWin.TouchBindEditVM.Action);
            }
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            int ind = Convert.ToInt32((sender as Button).Tag);
            if (ind >= 0 && ind != editorTestVM.SelectedActionSetIndex)
            {
                IsEnabled = false;

                editorTestVM.SwitchActionSets(ind);

                await Task.Run(() =>
                {
                    editorTestVM.ActionResetEvent.Wait();
                });

                DataContext = null;

                editorTestVM.RefreshSetBindings();

                DataContext = editorTestVM;

                IsEnabled = true;
            }
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            int ind = Convert.ToInt32((sender as Button).Tag);
            if (ind >= 0 && ind != editorTestVM.SelectedActionLayerIndex)
            {
                IsEnabled = false;

                editorTestVM.SwitchActionLayer(ind);

                await Task.Run(() =>
                {
                    editorTestVM.ActionResetEvent.Wait();
                });

                DataContext = null;

                editorTestVM.RefreshLayerBindings();

                DataContext = editorTestVM;

                IsEnabled = true;
            }
        }

        private async void Button_Click_4(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;

            editorTestVM.TestFakeSave(editorTestVM.ProfileEnt, editorTestVM.DeviceMapper.ActionProfile);

            await Task.Run(() =>
            {
                editorTestVM.ActionResetEvent.Wait();
            });

            IsEnabled = true;
        }

        private void AddLayerBtn_Click(object sender, RoutedEventArgs e)
        {
            editorTestVM.AddLayer();
        }

        private void RemoveLayerBtn_Click(object sender, RoutedEventArgs e)
        {
            // Skip for default ActionLayer
            if (editorTestVM.SelectedActionLayerIndex <= 0) return;

            editorTestVM.RemoveLayer();
            SwapActionLayer(editorTestVM.SelectedActionLayerIndex);
        }

        private void SwapActionLayer(int ind)
        {
            IsEnabled = false;

            editorTestVM.SwitchActionLayer(ind);

            editorTestVM.ActionResetEvent.Wait();

            DataContext = null;

            editorTestVM.RefreshLayerBindings();

            DataContext = editorTestVM;

            IsEnabled = true;
        }

        private void AddSetBtn_Click(object sender, RoutedEventArgs e)
        {
            editorTestVM.AddSet();
        }

        private void RemoveSetBtn_Click(object sender, RoutedEventArgs e)
        {
            // Skip for default ActionSet
            if (editorTestVM.SelectedActionSetIndex <= 0) return;

            editorTestVM.RemoveSet();
            SwapActionSet(editorTestVM.SelectedActionSetIndex);
        }

        private void SwapActionSet(int ind)
        {
            IsEnabled = false;

            editorTestVM.SwitchActionSets(ind);

            editorTestVM.ActionResetEvent.Wait();

            DataContext = null;

            editorTestVM.RefreshSetBindings();

            DataContext = editorTestVM;

            IsEnabled = true;
        }
    }
}
