using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ViewModels
{
    public class ButtonActionEditViewModel
    {
        private Dictionary<JoypadActionCodes, int> gamepadIndexAliases;
        private Dictionary<int, JoypadActionCodes> revGamepadIndexAliases;
        private List<GamepadCodeItem> gamepadComboItems;
        public List<GamepadCodeItem> GamepadComboItems => gamepadComboItems;

        private List<KeyboardCodeItem> keyboardComboItems;
        public List<KeyboardCodeItem> KeyboardComboItems => keyboardComboItems;

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
            slotItems = new ObservableCollection<OutputSlotItem>();

            PopulateGamepadAliases();
            revGamepadIndexAliases = new Dictionary<int, JoypadActionCodes>();
            foreach(KeyValuePair<JoypadActionCodes, int> pair in gamepadIndexAliases)
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
            foreach(OutputActionData data in func.OutputActions)
            {
                OutputActionData tempData = data;
                OutputSlotItem tempItem = new OutputSlotItem(tempData, tempInd++);
                slotItems.Add(tempItem);
            }

            //if (tempData.OutputType == OutputActionData.ActionType.GamepadControl)
            //{
            //    JoypadActionCodes temp = tempData.JoypadCode;
            //    selectedIndex = gamepadIndexAliases[temp];
            //}

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
        }

        private void DisconnectOutputSlotEvents()
        {
            SelectedIndexChanged -= ButtonActionEditViewModel_SelectedIndexChanged;
            SelectedKeyboardIndexChanged -= ButtonActionEditViewModel_SelectedKeyboardIndexChanged;
        }

        private void PrepareControlsForSlot(OutputSlotItem item)
        {
            SelectedKeyboardIndex = -1;
            SelectedIndex = -1;

            DisconnectOutputSlotEvents();

            if (item.Data.OutputType == OutputActionData.ActionType.GamepadControl)
            {
                JoypadActionCodes temp = item.Data.JoypadCode;
                SelectedIndex = gamepadIndexAliases[temp];
            }
            else if (item.Data.OutputType == OutputActionData.ActionType.Keyboard)
            {
                int keyInd = revKeyCodeDict[item.Data.OutputCode];
                SelectedKeyboardIndex = keyInd;
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

            SelectedIndex = -1;
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

            SelectedKeyboardIndex = -1;
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

        public void PopulateGamepadAliases()
        {
            int tempInd = 0;

            if (mapper.ActionProfile.OutputGamepadSettings.outputGamepad == EmulatedControllerSettings.OutputControllerType.Xbox360)
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
            SelectedSlotItemIndex = ind < slotItems.Count ? ind : slotItems.Count-1;
            mapper.QueueEvent(() =>
            {
                currentAction.Release(mapper, ignoreReleaseActions: true);
                func.OutputActions.RemoveAt(tempInd);
            });

            int tempSlotInd = 0;
            foreach(OutputSlotItem item in slotItems)
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
}
