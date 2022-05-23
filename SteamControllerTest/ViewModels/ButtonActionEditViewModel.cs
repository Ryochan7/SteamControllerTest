using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.MapperUtil;
using System.Threading;

namespace SteamControllerTest.ViewModels
{
    public class ButtonActionEditViewModel
    {
        public enum ActionComboBoxTypes
        {
            None,
            Gamepad,
            Keyboard,
            MouseButton,
            MouseWheelButton,
            RelativeMouseDir,
            LayerOp,
        }

        private Dictionary<JoypadActionCodes, int> gamepadIndexAliases;
        private Dictionary<int, JoypadActionCodes> revGamepadIndexAliases;
        private List<GamepadCodeItem> gamepadComboItems;
        public List<GamepadCodeItem> GamepadComboItems => gamepadComboItems;

        private List<KeyboardCodeItem> keyboardComboItems;
        public List<KeyboardCodeItem> KeyboardComboItems => keyboardComboItems;

        private List<MouseButtonCodeItem> mouseButtonComboItems;
        public List<MouseButtonCodeItem> MouseButtonComboItems => mouseButtonComboItems;

        private List<MouseButtonCodeItem> mouseWheelButtonComboItems;
        public List<MouseButtonCodeItem> MouseWheelButtonComboItems => mouseWheelButtonComboItems;

        private List<MouseDirItem> mouseDirComboItems;
        public List<MouseDirItem> MouseDirComboItems => mouseDirComboItems;

        private List<LayerOpChoiceItem> layerOperationsComboItems;
        public List<LayerOpChoiceItem> LayerOperationsComboItems => layerOperationsComboItems;

        private List<AvailableLayerChoiceItem> availableLayerComboItems;
        public List<AvailableLayerChoiceItem> AvailableLayerComboItems => availableLayerComboItems;

        // Keycode, Index
        private Dictionary<int, int> revKeyCodeDict = new Dictionary<int, int>();

        private ButtonAction currentAction;
        public ButtonAction CurrentAction
        {
            get => currentAction;
        }

        private Mapper mapper;
        private ActionFunc func;

        private int selectedIndex = -1;
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedIndexChanged;

        private int selectedKeyboardIndex = -1;
        public int SelectedKeyboardIndex
        {
            get => selectedKeyboardIndex;
            set
            {
                selectedKeyboardIndex = value;
                SelectedKeyboardIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedKeyboardIndexChanged;

        private int selectedMouseButtonIndex = -1;
        public int SelectedMouseButtonIndex
        {
            get => selectedMouseButtonIndex;
            set
            {
                selectedMouseButtonIndex = value;
                SelectedMouseButtonIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedMouseButtonIndexChanged;

        private int selectedMouseWheelButtonIndex = -1;
        public int SelectedMouseWheelButtonIndex
        {
            get => selectedMouseWheelButtonIndex;
            set
            {
                selectedMouseWheelButtonIndex = value;
                SelectedMouseWheelButtonIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedMouseWheelButtonIndexChanged;

        private int selectedMouseDirIndex = -1;
        public int SelectedMouseDirIndex
        {
            get => selectedMouseDirIndex;
            set
            {
                selectedMouseDirIndex = value;
                SelectedMouseDirIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedMouseDirIndexChanged;

        private int selectedLayerOpsIndex = -1;
        public int SelectedLayerOpsIndex
        {
            get => selectedLayerOpsIndex;
            set
            {
                selectedLayerOpsIndex = value;
                SelectedLayerOpsIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedLayerOpsIndexChanged;

        private int selectedLayerChoiceIndex = -1;
        public int SelectedLayerChoiceIndex
        {
            get => selectedLayerChoiceIndex;
            set
            {
                selectedLayerChoiceIndex = value;
                SelectedLayerChoiceIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedLayerChoiceIndexChanged;

        private bool showAvailableLayers;
        public bool ShowAvailableLayers
        {
            get => showAvailableLayers;
            set
            {
                showAvailableLayers = value;
                ShowAvailableLayersChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ShowAvailableLayersChanged;

        private int selectedLayerChangeConditionIndex = -1;
        public int SelectedLayerChangeConditionIndex
        {
            get => selectedLayerChangeConditionIndex;
            set
            {
                selectedLayerChangeConditionIndex = value;
                SelectedLayerChangeConditionIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedLayerChangeConditionIndexChanged;

        private ObservableCollection<OutputSlotItem> slotItems;
        public ObservableCollection<OutputSlotItem> SlotItems => slotItems;

        private int selectedSlotItemIndex = -1;
        public int SelectedSlotItemIndex
        {
            get => selectedSlotItemIndex;
            set
            {
                selectedSlotItemIndex = value;
                SelectedSlotItemIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedSlotItemIndexChanged;

        public bool HasMultipleSlots
        {
            get => slotItems.Count > 1;
        }
        public event EventHandler HasMultipleSlotsChanged;

        public ButtonActionEditViewModel(Mapper mapper, ButtonAction currentAction, ActionFunc func)
        {
            this.currentAction = currentAction;
            this.mapper = mapper;
            this.func = func;

            gamepadComboItems = new List<GamepadCodeItem>();
            keyboardComboItems = new List<KeyboardCodeItem>();
            mouseButtonComboItems = new List<MouseButtonCodeItem>();
            mouseWheelButtonComboItems = new List<MouseButtonCodeItem>();
            mouseDirComboItems = new List<MouseDirItem>();
            layerOperationsComboItems = new List<LayerOpChoiceItem>();
            availableLayerComboItems = new List<AvailableLayerChoiceItem>();
            slotItems = new ObservableCollection<OutputSlotItem>();

            PopulateComboBoxAliases();
            revGamepadIndexAliases = new Dictionary<int, JoypadActionCodes>();
            foreach (KeyValuePair<JoypadActionCodes, int> pair in gamepadIndexAliases)
            {
                revGamepadIndexAliases.Add(pair.Value, pair.Key);
            }

            int tempKeyInd = 0;
            keyboardComboItems.ForEach((item) =>
            {
                int tempCode = (int)ProfileSerializer.FakerInputMapper.GetRealEventKey((uint)item.Code);
                revKeyCodeDict.Add(tempCode, tempKeyInd++);
            });

            int tempInd = 0;
            foreach (OutputActionData data in func.OutputActions)
            {
                OutputActionData tempData = data;
                OutputSlotItem tempItem = new OutputSlotItem(tempData, tempInd++);
                slotItems.Add(tempItem);
            }

            if (slotItems.Count > 0)
            {
                PrepareControlsForSlot(slotItems[0]);
                SelectedSlotItemIndex = 0;
            }

            SetupEvents();
        }

        private void ButtonActionEditViewModel_SelectedSlotItemIndexChanged(object sender, EventArgs e)
        {
            OutputSlotItem item = slotItems[selectedSlotItemIndex];
            PrepareControlsForSlot(item);
        }

        private void SetupEvents()
        {
            ConnectOutputSlotEvents();

            SelectedSlotItemIndexChanged += ButtonActionEditViewModel_SelectedSlotItemIndexChanged;
            slotItems.CollectionChanged += SlotItems_CollectionChanged;
        }

        private void SlotItems_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            HasMultipleSlotsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ConnectOutputSlotEvents()
        {
            SelectedIndexChanged += ButtonActionEditViewModel_SelectedIndexChanged;
            SelectedKeyboardIndexChanged += ButtonActionEditViewModel_SelectedKeyboardIndexChanged;
            SelectedMouseButtonIndexChanged += ButtonActionEditViewModel_SelectedMouseButtonIndexChanged;
            SelectedMouseWheelButtonIndexChanged += ButtonActionEditViewModel_SelectedMouseWheelButtonIndexChanged;
            SelectedMouseDirIndexChanged += ButtonActionEditViewModel_SelectedMouseDirIndexChanged;
            SelectedLayerOpsIndexChanged += ButtonActionEditViewModel_SelectedLayerOpsIndexChanged;
            SelectedLayerChoiceIndexChanged += ButtonActionEditViewModel_SelectedLayerChoiceIndexChanged;
            SelectedLayerChangeConditionIndexChanged += ButtonActionEditViewModel_SelectedLayerChangeConditionIndexChanged;
        }

        private void ButtonActionEditViewModel_SelectedMouseDirIndexChanged(object sender, EventArgs e)
        {
            int index = selectedMouseDirIndex;
            if (index == -1) return;

            ResetComboBoxIndex(ActionComboBoxTypes.RelativeMouseDir);
            MouseDirItem item = mouseDirComboItems[index];
            ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);
            mapper.QueueEvent(() =>
            {
                currentAction.Release(mapper, ignoreReleaseActions: true);
                OutputSlotItem slotItem = slotItems[selectedSlotItemIndex];
                OutputActionData tempData = slotItem.Data;
                tempData.Reset();

                OutputActionData.RelativeMouseDir tempDir = (OutputActionData.RelativeMouseDir)item.Code;
                tempData.Prepare(OutputActionData.ActionType.RelativeMouse, 0);
                tempData.mouseDir = tempDir;
                tempData.OutputCodeStr = tempDir.ToString();

                resetEvent.Set();
            });

            resetEvent.Wait();
        }

        private void ButtonActionEditViewModel_SelectedMouseWheelButtonIndexChanged(object sender, EventArgs e)
        {
            int index = selectedMouseWheelButtonIndex;
            if (index == -1)
            {
                return;
            }

            ResetComboBoxIndex(ActionComboBoxTypes.MouseWheelButton);
            MouseButtonCodeItem item = mouseWheelButtonComboItems[index];
            ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);
            mapper.QueueEvent(() =>
            {
                currentAction.Release(mapper, ignoreReleaseActions: true);
                OutputSlotItem slotItem = slotItems[selectedSlotItemIndex];
                OutputActionData tempData = slotItem.Data;
                tempData.Reset();

                tempData.Prepare(OutputActionData.ActionType.MouseWheel, item.Code);
                tempData.OutputCodeStr = ((OutputActionDataSerializer.MouseWheelAliases)item.Code).ToString();

                resetEvent.Set();
            });

            resetEvent.Wait();
        }

        private void ButtonActionEditViewModel_SelectedLayerChangeConditionIndexChanged(object sender, EventArgs e)
        {
            int index = selectedLayerChangeConditionIndex;
            if (index == -1) return;

            ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);
            mapper.QueueEvent(() =>
            {
                currentAction.Release(mapper, ignoreReleaseActions: true);
                OutputSlotItem slotItem = slotItems[selectedSlotItemIndex];
                OutputActionData tempData = slotItem.Data;
                tempData.LayerChangeCondition = (OutputActionData.ActionLayerChangeCondition)index;

                resetEvent.Set();
            });

            resetEvent.Wait();
        }

        private void ButtonActionEditViewModel_SelectedLayerChoiceIndexChanged(object sender, EventArgs e)
        {
            int index = selectedLayerChoiceIndex;
            if (index == -1) return;

            AvailableLayerChoiceItem tempItem = availableLayerComboItems[index];
            LayerOpChoiceItem opItem = layerOperationsComboItems[selectedLayerOpsIndex];
            ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);
            mapper.QueueEvent(() =>
            {
                currentAction.Release(mapper, ignoreReleaseActions: true);
                OutputSlotItem slotItem = slotItems[selectedSlotItemIndex];
                OutputActionData tempData = slotItem.Data;

                tempData.Reset();
                tempData.Prepare(opItem.LayerOp, 0);
                tempData.ChangeToLayer = tempItem.Layer.Index;
                if (opItem.LayerOp != OutputActionData.ActionType.HoldActionLayer &&
                    selectedLayerChangeConditionIndex >= 0)
                {
                    tempData.LayerChangeCondition = (OutputActionData.ActionLayerChangeCondition)selectedLayerChangeConditionIndex;
                }

                resetEvent.Set();
            });

            resetEvent.Wait();
        }

        private void ButtonActionEditViewModel_SelectedLayerOpsIndexChanged(object sender, EventArgs e)
        {
            int index = selectedLayerOpsIndex;
            if (index == -1) return;

            ResetComboBoxIndex(ActionComboBoxTypes.LayerOp);
            ShowAvailableLayers = true;

            LayerOpChoiceItem tempItem = layerOperationsComboItems[index];
            switch(tempItem.LayerOp)
            {
                case OutputActionData.ActionType.HoldActionLayer:
                    SelectedLayerChangeConditionIndex = -1;
                    break;
                case OutputActionData.ActionType.ApplyActionLayer:
                case OutputActionData.ActionType.SwitchActionLayer:
                    SelectedLayerChangeConditionIndex = 1;
                    break;
                case OutputActionData.ActionType.RemoveActionLayer:
                    SelectedLayerChangeConditionIndex = 2;
                    break;
                default:
                    break;
            }
        }

        private void DisconnectOutputSlotEvents()
        {
            SelectedIndexChanged -= ButtonActionEditViewModel_SelectedIndexChanged;
            SelectedKeyboardIndexChanged -= ButtonActionEditViewModel_SelectedKeyboardIndexChanged;
            SelectedMouseButtonIndexChanged -= ButtonActionEditViewModel_SelectedMouseButtonIndexChanged;
            SelectedMouseWheelButtonIndexChanged -= ButtonActionEditViewModel_SelectedMouseWheelButtonIndexChanged;
            SelectedMouseDirIndexChanged -= ButtonActionEditViewModel_SelectedMouseDirIndexChanged;
            SelectedLayerOpsIndexChanged -= ButtonActionEditViewModel_SelectedLayerOpsIndexChanged;
            SelectedLayerChoiceIndexChanged -= ButtonActionEditViewModel_SelectedLayerChoiceIndexChanged;
        }

        private void PrepareControlsForSlot(OutputSlotItem item)
        {
            ResetComboBoxIndex(ActionComboBoxTypes.None);

            DisconnectOutputSlotEvents();

            switch (item.Data.OutputType)
            {
                case OutputActionData.ActionType.GamepadControl:
                    {
                        JoypadActionCodes temp = item.Data.JoypadCode;
                        SelectedIndex = gamepadIndexAliases[temp];
                    }

                    break;
                case OutputActionData.ActionType.Keyboard:
                    {
                        int keyInd = revKeyCodeDict[item.Data.OutputCode];
                        SelectedKeyboardIndex = keyInd;
                    }

                    break;
                case OutputActionData.ActionType.MouseButton:
                    {
                        int code = item.Data.OutputCode;
                        MouseButtonCodeItem tempMBItem = mouseButtonComboItems.FirstOrDefault((item) => item.Code == code);
                        if (tempMBItem != null)
                        {
                            SelectedMouseButtonIndex = tempMBItem.Index;
                        }
                    }

                    break;
                case OutputActionData.ActionType.MouseWheel:
                    {
                        int code = item.Data.OutputCode;
                        MouseButtonCodeItem tempWheelItem = mouseWheelButtonComboItems.FirstOrDefault((item) => item.Code == code);
                        if (tempWheelItem != null)
                        {
                            SelectedMouseWheelButtonIndex = tempWheelItem.Index;
                        }
                    }

                    break;
                case OutputActionData.ActionType.RelativeMouse:
                    {
                        OutputActionData.RelativeMouseDir tempDir = item.Data.mouseDir;
                        int dirCode = (int)tempDir;
                        MouseDirItem tempItem = mouseDirComboItems.FirstOrDefault((item) => item.Code == dirCode);
                        if (tempItem != null)
                        {
                            SelectedMouseDirIndex = tempItem.Index;
                        }
                    }
                    break;
                case OutputActionData.ActionType.HoldActionLayer:
                case OutputActionData.ActionType.ApplyActionLayer:
                case OutputActionData.ActionType.RemoveActionLayer:
                case OutputActionData.ActionType.SwitchActionLayer:
                    {
                        int ind = layerOperationsComboItems.FindIndex((opItem) => opItem.LayerOp == item.Data.OutputType);
                        if (ind >= 0)
                        {
                            SelectedLayerOpsIndex = ind;
                            SelectedLayerChoiceIndex = item.Data.ChangeToLayer;
                            ShowAvailableLayers = true;

                            if (item.Data.OutputType != OutputActionData.ActionType.HoldActionLayer)
                            {
                                SelectedLayerChangeConditionIndex = (int)item.Data.ChangeCondition;
                            }
                        }
                    }

                    break;
                default:
                    break;
            }

            ConnectOutputSlotEvents();
        }

        private void ButtonActionEditViewModel_SelectedKeyboardIndexChanged(object sender, EventArgs e)
        {
            int index = selectedKeyboardIndex;
            if (index == -1)
            {
                return;
            }

            ResetComboBoxIndex(ActionComboBoxTypes.Keyboard);
            KeyboardCodeItem item = keyboardComboItems[index];
            mapper.QueueEvent(() =>
            {
                currentAction.Release(mapper, ignoreReleaseActions: true);
                OutputSlotItem slotItem = slotItems[selectedSlotItemIndex];
                OutputActionData tempData = slotItem.Data;
                if (tempData.OutputType == OutputActionData.ActionType.Keyboard)
                {
                    tempData.Reset();

                    int tempCode = (int)ProfileSerializer.FakerInputMapper.GetRealEventKey((uint)item.Code);
                    tempData.Prepare(OutputActionData.ActionType.Keyboard, tempCode);
                    tempData.OutputCodeStr = item.CodeAlias;
                }
                else
                {
                    tempData.Reset();

                    int tempCode = (int)ProfileSerializer.FakerInputMapper.GetRealEventKey((uint)item.Code);
                    //tempData = new OutputActionData(OutputActionData.ActionType.Keyboard, tempCode);
                    tempData.Prepare(OutputActionData.ActionType.Keyboard, tempCode);
                    tempData.OutputCodeStr = item.CodeAlias;
                }
            });
        }

        private void ButtonActionEditViewModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = selectedIndex;
            if (index == -1)
            {
                return;
            }

            ResetComboBoxIndex(ActionComboBoxTypes.Gamepad);
            JoypadActionCodes temp = revGamepadIndexAliases[index];
            OutputSlotItem item = slotItems[selectedSlotItemIndex];
            mapper.QueueEvent(() =>
            {
                currentAction.Release(mapper, ignoreReleaseActions: true);
                //currentAction.ActionFuncs[0].Release(mapper);

                OutputActionData tempData = item.Data;
                if (tempData.OutputType == OutputActionData.ActionType.GamepadControl)
                {
                    tempData.Reset();

                    tempData.OutputType = OutputActionData.ActionType.GamepadControl;
                    tempData.JoypadCode = temp;
                }
                else
                {
                    tempData.Reset();

                    tempData.OutputType = OutputActionData.ActionType.GamepadControl;
                    tempData.JoypadCode = temp;
                    //tempData =
                    //    new OutputActionData(OutputActionData.ActionType.GamepadControl, temp);
                }
            });
        }

        private void ButtonActionEditViewModel_SelectedMouseButtonIndexChanged(object sender, EventArgs e)
        {
            int index = selectedMouseButtonIndex;
            if (index == -1)
            {
                return;
            }

            ResetComboBoxIndex(ActionComboBoxTypes.MouseButton);
            MouseButtonCodeItem item = mouseButtonComboItems[index];
            ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);
            mapper.QueueEvent(() =>
            {
                currentAction.Release(mapper, ignoreReleaseActions: true);
                OutputSlotItem slotItem = slotItems[selectedSlotItemIndex];
                OutputActionData tempData = slotItem.Data;
                tempData.Reset();

                tempData.Prepare(OutputActionData.ActionType.MouseButton, item.Code);
                tempData.OutputCodeStr = ((OutputActionDataSerializer.MouseButtonAliases)item.Code).ToString();

                resetEvent.Set();
            });

            resetEvent.Wait();
        }

        private void ResetComboBoxIndex(ActionComboBoxTypes ignoreCombo)
        {
            if (ignoreCombo != ActionComboBoxTypes.Gamepad)
            {
                SelectedIndex = -1;
            }

            if (ignoreCombo != ActionComboBoxTypes.Keyboard)
            {
                SelectedKeyboardIndex = -1;
            }

            if (ignoreCombo != ActionComboBoxTypes.MouseButton)
            {
                SelectedMouseButtonIndex = -1;
            }

            if (ignoreCombo != ActionComboBoxTypes.MouseWheelButton)
            {
                SelectedMouseWheelButtonIndex = -1;
            }

            if (ignoreCombo != ActionComboBoxTypes.RelativeMouseDir)
            {
                SelectedMouseDirIndex = -1;
            }

            if (ignoreCombo != ActionComboBoxTypes.LayerOp)
            {
                SelectedLayerOpsIndex = -1;
                SelectedLayerChoiceIndex = -1;
                ShowAvailableLayers = false;
                SelectedLayerChangeConditionIndex = -1;
            }
        }

        public void PopulateComboBoxAliases()
        {
            int tempInd = 0;

            //if (mapper.ActionProfile.OutputGamepadSettings.outputGamepad == EmulatedControllerSettings.OutputControllerType.Xbox360)
            {
                gamepadIndexAliases = new Dictionary<JoypadActionCodes, int>()
                {
                    [JoypadActionCodes.Empty] = 0,
                    [JoypadActionCodes.X360_A] = 1,
                    [JoypadActionCodes.X360_B] = 2,
                    [JoypadActionCodes.X360_X] = 3,
                    [JoypadActionCodes.X360_Y] = 4,
                    [JoypadActionCodes.X360_LB] = 5,
                    [JoypadActionCodes.X360_RB] = 6,
                    [JoypadActionCodes.X360_Guide] = 7,
                    [JoypadActionCodes.X360_Back] = 8,
                    [JoypadActionCodes.X360_Start] = 9,
                    [JoypadActionCodes.X360_ThumbL] = 10,
                    [JoypadActionCodes.X360_ThumbR] = 11,
                };

                tempInd = 0;
                gamepadComboItems.AddRange(new GamepadCodeItem[]
                {
                    new GamepadCodeItem("Unbound", JoypadActionCodes.Empty, 0),
                    new GamepadCodeItem("X360_A", JoypadActionCodes.X360_A, 1),
                    new GamepadCodeItem("X360_B", JoypadActionCodes.X360_B, 2),
                    new GamepadCodeItem("X360_X", JoypadActionCodes.X360_X, 3),
                    new GamepadCodeItem("X360_Y", JoypadActionCodes.X360_Y, 4),
                    new GamepadCodeItem("X360_LB", JoypadActionCodes.X360_LB, 5),
                    new GamepadCodeItem("X360_RB", JoypadActionCodes.X360_RB, 6),
                    new GamepadCodeItem("X360_Guide", JoypadActionCodes.X360_Guide, 7),
                    new GamepadCodeItem("X360_Back", JoypadActionCodes.X360_Back, 8),
                    new GamepadCodeItem("X360_Start", JoypadActionCodes.X360_Start, 9),
                    new GamepadCodeItem("X360_ThumbL", JoypadActionCodes.X360_ThumbL, 10),
                    new GamepadCodeItem("X360_ThumbR", JoypadActionCodes.X360_ThumbR, 11),
                });
            }

            tempInd = 0;
            keyboardComboItems.AddRange(new KeyboardCodeItem[]
            {
                new KeyboardCodeItem("A", VirtualKeys.A, "A", tempInd++),
                new KeyboardCodeItem("B", VirtualKeys.B, "B", tempInd++),
                new KeyboardCodeItem("C", VirtualKeys.C, "C", tempInd++),
                new KeyboardCodeItem("D", VirtualKeys.D, "D", tempInd++),
                new KeyboardCodeItem("E", VirtualKeys.E, "E", tempInd++),
                new KeyboardCodeItem("F", VirtualKeys.F, "F", tempInd++),
                new KeyboardCodeItem("G", VirtualKeys.G, "G", tempInd++),
                new KeyboardCodeItem("H", VirtualKeys.H, "H", tempInd++),
                new KeyboardCodeItem("I", VirtualKeys.I, "I", tempInd++),
                new KeyboardCodeItem("J", VirtualKeys.J, "J", tempInd++),
                new KeyboardCodeItem("K", VirtualKeys.K, "K", tempInd++),
                new KeyboardCodeItem("L", VirtualKeys.L, "L", tempInd++),
                new KeyboardCodeItem("M", VirtualKeys.M, "M", tempInd++),
                new KeyboardCodeItem("N", VirtualKeys.N, "N", tempInd++),
                new KeyboardCodeItem("O", VirtualKeys.O, "O", tempInd++),
                new KeyboardCodeItem("P", VirtualKeys.P, "P", tempInd++),
                new KeyboardCodeItem("Q", VirtualKeys.Q, "Q", tempInd++),
                new KeyboardCodeItem("R", VirtualKeys.R, "R", tempInd++),
                new KeyboardCodeItem("S", VirtualKeys.S, "S", tempInd++),
                new KeyboardCodeItem("T", VirtualKeys.T, "T", tempInd++),
                new KeyboardCodeItem("U", VirtualKeys.U, "U", tempInd++),
                new KeyboardCodeItem("V", VirtualKeys.V, "V", tempInd++),
                new KeyboardCodeItem("W", VirtualKeys.W, "W", tempInd++),
                new KeyboardCodeItem("X", VirtualKeys.X, "X", tempInd++),
                new KeyboardCodeItem("Y", VirtualKeys.Y, "Y", tempInd++),
                new KeyboardCodeItem("Z", VirtualKeys.Z, "Z", tempInd++),
                new KeyboardCodeItem("0", VirtualKeys.N0, "N0", tempInd++),
                new KeyboardCodeItem("1", VirtualKeys.N1, "N1", tempInd++),
                new KeyboardCodeItem("2", VirtualKeys.N2, "N2", tempInd++),
                new KeyboardCodeItem("3", VirtualKeys.N3, "N3", tempInd++),
                new KeyboardCodeItem("4", VirtualKeys.N4, "N4", tempInd++),
                new KeyboardCodeItem("5", VirtualKeys.N5, "N5", tempInd++),
                new KeyboardCodeItem("6", VirtualKeys.N6, "N6", tempInd++),
                new KeyboardCodeItem("7", VirtualKeys.N7, "N7", tempInd++),
                new KeyboardCodeItem("8", VirtualKeys.N8, "N8", tempInd++),
                new KeyboardCodeItem("9", VirtualKeys.N9, "N9", tempInd++),
                new KeyboardCodeItem("Escape", VirtualKeys.Escape, "Escape", tempInd++),
                new KeyboardCodeItem("Tab", VirtualKeys.Tab, "Tab", tempInd++),
                new KeyboardCodeItem("Space", VirtualKeys.Space, "Tab", tempInd++),
                new KeyboardCodeItem("Left Alt", VirtualKeys.LeftMenu, "LeftAlt", tempInd++),
                new KeyboardCodeItem("Right Alt", VirtualKeys.RightMenu, "RightAlt", tempInd++),
                new KeyboardCodeItem("Left Shift", VirtualKeys.LeftShift, "LeftShift", tempInd++),
                new KeyboardCodeItem("Right Shift", VirtualKeys.RightShift, "RightShift", tempInd++),
                new KeyboardCodeItem("Left Control", VirtualKeys.LeftControl, "LeftControl", tempInd++),
                new KeyboardCodeItem("Right Control", VirtualKeys.RightControl, "RightControl", tempInd++),
            });

            tempInd = 0;
            mouseButtonComboItems.AddRange(new MouseButtonCodeItem[]
            {
                new MouseButtonCodeItem("Left Button", MouseButtonCodes.MOUSE_LEFT_BUTTON, tempInd++),
                new MouseButtonCodeItem("Right Button", MouseButtonCodes.MOUSE_RIGHT_BUTTON, tempInd++),
                new MouseButtonCodeItem("Middle Button", MouseButtonCodes.MOUSE_MIDDLE_BUTTON, tempInd++),
                new MouseButtonCodeItem("XButton1", MouseButtonCodes.MOUSE_XBUTTON1, tempInd++),
                new MouseButtonCodeItem("XButton2", MouseButtonCodes.MOUSE_XBUTTON2, tempInd++),
            });

            tempInd = 0;
            mouseWheelButtonComboItems.AddRange(new MouseButtonCodeItem[]
            {
                new MouseButtonCodeItem("Wheel Up", (int)MouseWheelCodes.WheelUp, tempInd++),
                new MouseButtonCodeItem("Wheel Down", (int)MouseWheelCodes.WheelDown, tempInd++),
                new MouseButtonCodeItem("Wheel Left", (int)MouseWheelCodes.WheelLeft, tempInd++),
                new MouseButtonCodeItem("Wheel Right", (int)MouseWheelCodes.WheelRight, tempInd++),
            });

            tempInd = 0;
            mouseDirComboItems.AddRange(new MouseDirItem[]
            {
                new MouseDirItem("Mouse Up", (int)OutputActionData.RelativeMouseDir.MouseUp, tempInd++),
                new MouseDirItem("Mouse Down", (int)OutputActionData.RelativeMouseDir.MouseDown, tempInd++),
                new MouseDirItem("Mouse Left", (int)OutputActionData.RelativeMouseDir.MouseLeft, tempInd++),
                new MouseDirItem("Mouse Right", (int)OutputActionData.RelativeMouseDir.MouseRight, tempInd++),
            });

            tempInd = 0;
            layerOperationsComboItems.AddRange(new LayerOpChoiceItem[]
            {
                new LayerOpChoiceItem("Hold Layer", OutputActionData.ActionType.HoldActionLayer, tempInd++),
                new LayerOpChoiceItem("Apply Layer", OutputActionData.ActionType.ApplyActionLayer, tempInd++),
                new LayerOpChoiceItem("Remove Layer", OutputActionData.ActionType.RemoveActionLayer, tempInd++),
                new LayerOpChoiceItem("Switch Layer", OutputActionData.ActionType.SwitchActionLayer, tempInd++),
            });

            tempInd = 0;
            mapper.ActionProfile.CurrentActionSet.ActionLayers.ForEach((layer) =>
            {
                AvailableLayerChoiceItem tempChoiceItem = new AvailableLayerChoiceItem(layer, tempInd++);
                availableLayerComboItems.Add(tempChoiceItem);
            });
        }

        public void AddTempOutputSlot()
        {
            int ind = slotItems.Count;
            OutputActionData tempData = new OutputActionData(OutputActionData.ActionType.Empty, 0);
            OutputSlotItem item = new OutputSlotItem(tempData, ind);
            slotItems.Add(item);

            SelectedSlotItemIndex = ind;

            mapper.QueueEvent(() =>
            {
                currentAction.Release(mapper, ignoreReleaseActions: true);
                func.OutputActions.Add(tempData);
            });

            //PrepareControlsForSlot(item);
        }

        public void RemoveOutputSlot(int ind)
        {
            if (slotItems.Count == 1)
            {
                return;
            }
            else if (ind >= slotItems.Count)
            {
                return;
            }

            int tempInd = ind;
            slotItems.RemoveAt(tempInd);
            SelectedSlotItemIndex = ind < slotItems.Count ? ind : slotItems.Count - 1;
            mapper.QueueEvent(() =>
            {
                currentAction.Release(mapper, ignoreReleaseActions: true);
                func.OutputActions.RemoveAt(tempInd);
            });

            int tempSlotInd = 0;
            foreach (OutputSlotItem item in slotItems)
            {
                item.SlotIndex = tempSlotInd++;
            }
        }
    }

    public class OutputSlotItem
    {
        private OutputActionData data;
        public OutputActionData Data
        {
            get => data;
            set => data = value;
        }

        public string DisplayName
        {
            get
            {
                return index.ToString();
            }
        }

        private int index;
        public int SlotIndex
        {
            get => index;
            set => index = value;
        }

        public OutputSlotItem(OutputActionData data, int index)
        {
            this.data = data;
            this.index = index;
        }
    }

    public class GamepadCodeItem
    {
        private string displayName;
        public string DisplayName => displayName;

        private JoypadActionCodes code;
        public JoypadActionCodes Code => code;

        private int index;
        public int Index => index;

        public GamepadCodeItem(string displayName, JoypadActionCodes code, int index)
        {
            this.displayName = displayName;
            this.code = code;
            this.index = index;
        }
    }

    public class KeyboardCodeItem
    {
        private string displayName;
        public string DisplayName => displayName;

        private VirtualKeys code;
        public VirtualKeys Code => code;

        private int index;
        public int Index => index;

        private string codeAlias;
        public string CodeAlias => codeAlias;

        public KeyboardCodeItem(string displayName, VirtualKeys code, string codeAlias, int index)
        {
            this.displayName = displayName;
            this.code = code;
            this.codeAlias = codeAlias;
            this.index = index;
        }
    }

    public class MouseButtonCodeItem
    {
        private string displayName;
        public string DisplayName => displayName;

        private int code;
        public int Code => code;

        private int index;
        public int Index => index;

        public MouseButtonCodeItem(string displayName, int code, int index)
        {
            this.displayName = displayName;
            this.code = code;
            this.index = index;
        }
    }

    public class MouseDirItem
    {
        private string displayName;
        public string DisplayName => displayName;

        private int code;
        public int Code => code;

        private int index;
        public int Index => index;

        public MouseDirItem(string displayName, int code, int index)
        {
            this.displayName = displayName;
            this.code = code;
            this.index = index;
        }
    }

    public class LayerOpChoiceItem
    {
        private string displayName;
        public string DisplayName => displayName;

        private OutputActionData.ActionType layerOp;
        public OutputActionData.ActionType LayerOp => layerOp;

        private int index;
        public int Index => index;

        public LayerOpChoiceItem(string displayName, OutputActionData.ActionType layerOp,
            int index)
        {
            this.displayName = displayName;
            this.layerOp = layerOp;
            this.index = index;
        }
    }

    public class AvailableLayerChoiceItem
    {
        private string displayName;
        public string DisplayName => displayName;

        private ActionLayer layer;
        public ActionLayer Layer => layer;

        private int index;
        public int Index => index;

        public AvailableLayerChoiceItem(ActionLayer layer, int index)
        {
            this.layer = layer;
            this.index = index;

            if (!string.IsNullOrEmpty(layer.Name))
            {
                displayName = $"{layer.Name} ({index})";
            }
            else
            {
                displayName = $"{index}";
            }
        }
    }
}
