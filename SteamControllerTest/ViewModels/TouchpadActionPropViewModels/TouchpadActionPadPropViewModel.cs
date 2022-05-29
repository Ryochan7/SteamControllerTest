using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.TouchpadActions;

namespace SteamControllerTest.ViewModels.TouchpadActionPropViewModels
{
    public class TouchpadActionPadPropViewModel : TouchpadActionPropVMBase
    {
        private TouchpadActionPad action;
        public TouchpadActionPad Action
        {
            get => action;
        }

        public string DeadZone
        {
            get => action.DeadMod.DeadZone.ToString();
            set
            {
                if (double.TryParse(value, out double temp))
                {
                    action.DeadMod.DeadZone = Math.Clamp(temp, 0.0, 1.0);
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                    ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler DeadZoneChanged;

        public int DiagonalRange
        {
            get => action.DiagonalRange;
            set
            {
                if (action.DiagonalRange == value) return;
                action.DiagonalRange = value;
                DiagonalRangeChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DiagonalRangeChanged;

        public bool RequiresClick
        {
            get => action.RequiresClick;
            set
            {
                if (action.RequiresClick == value) return;
                action.RequiresClick = value;
                RequiresClickChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RequiresClickChanged;

        private List<PadModeItem> padModeItems;
        public List<PadModeItem> PadModeItems => padModeItems;

        private int selectedPadModeIndex = -1;
        public int SelectedPadModeIndex
        {
            get => selectedPadModeIndex;
            set
            {
                if (selectedPadModeIndex == value) return;
                selectedPadModeIndex = value;
                SelectedPadModeIndexChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedPadModeIndexChanged;

        public bool ShowDiagonalPad
        {
            get => action.CurrentMode == TouchpadActionPad.DPadMode.EightWay ||
                action.CurrentMode == TouchpadActionPad.DPadMode.FourWayDiagonal;
        }
        public event EventHandler ShowDiagonalPadChanged;

        public bool ShowCardinalPad
        {
            get => action.CurrentMode == TouchpadActionPad.DPadMode.Standard ||
                action.CurrentMode == TouchpadActionPad.DPadMode.EightWay ||
                action.CurrentMode == TouchpadActionPad.DPadMode.FourWayCardinal;
        }
        public event EventHandler ShowCardinalPadChanged;

        public string ActionUpBtnDisplayBind
        {
            get => action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Up].DescribeActions();
        }

        public string ActionDownBtnDisplayBind
        {
            get => action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Down].DescribeActions();
        }

        public string ActionLeftBtnDisplayBind
        {
            get => action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Left].DescribeActions();
        }

        public string ActionRightBtnDisplayBind
        {
            get => action.EventCodes4[(int)TouchpadActionPad.DpadDirections.Right].DescribeActions();
        }

        public string ActionUpLeftBtnDisplayBind
        {
            get => action.EventCodes4[(int)TouchpadActionPad.DpadDirections.UpLeft].DescribeActions();
        }

        public string ActionUpRightBtnDisplayBind
        {
            get => action.EventCodes4[(int)TouchpadActionPad.DpadDirections.UpRight].DescribeActions();
        }

        public string ActionDownLeftBtnDisplayBind
        {
            get => action.EventCodes4[(int)TouchpadActionPad.DpadDirections.DownLeft].DescribeActions();
        }

        public string ActionDownRightBtnDisplayBind
        {
            get => action.EventCodes4[(int)TouchpadActionPad.DpadDirections.DownRight].DescribeActions();
        }

        public bool HighlightName
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.NAME);
        }
        public event EventHandler HighlightNameChanged;

        public bool HighlightDeadZone
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.DEAD_ZONE);
        }
        public event EventHandler HighlightDeadZoneChanged;

        public bool HighlightDiagonalRange
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.DIAGONAL_RANGE);
        }
        public event EventHandler HighlightDiagonalRangeChanged;

        public bool HighlightRequiresClick
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.REQUIRES_CLICK);
        }
        public event EventHandler HighlightRequiresClickChanged;

        public bool HighlightPadMode
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.PAD_MODE);
        }
        public event EventHandler HighlightPadModeChanged;

        public override event EventHandler ActionPropertyChanged;

        public TouchpadActionPadPropViewModel(Mapper mapper, TouchpadMapAction action)
        {
            this.mapper = mapper;
            this.action = action as TouchpadActionPad;
            this.baseAction = action;

            padModeItems = new List<PadModeItem>();

            // Check if base ActionLayer action from composite layer
            if (action.ParentAction == null &&
                mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer &&
                !mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.LayerActions.Contains(action) &&
                MapAction.IsSameType(mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId], action))
            {
                // Test with temporary object
                TouchpadActionPad baseLayerAction = mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId] as TouchpadActionPad;
                TouchpadActionPad tempAction = new TouchpadActionPad();
                tempAction.SoftCopyFromParent(baseLayerAction);
                //int tempLayerId = mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.Index;
                int tempId = mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.FindNextAvailableId();
                tempAction.Id = tempId;
                //tempAction.MappingId = this.action.MappingId;

                this.action = tempAction;
                this.baseAction = action;

                ActionPropertyChanged += ReplaceExistingLayerAction;
            }

            PrepareModel();

            NameChanged += TouchpadActionPadPropViewModel_NameChanged;
            DeadZoneChanged += TouchpadActionPadPropViewModel_DeadZoneChanged;
            DiagonalRangeChanged += TouchpadActionPadPropViewModel_DiagonalRangeChanged;
            RequiresClickChanged += TouchpadActionPadPropViewModel_RequiresClickChanged;
            SelectedPadModeIndexChanged += ChangeStickPadMode;
            SelectedPadModeIndexChanged += TouchpadActionPadPropViewModel_SelectedPadModeIndexChanged;
        }

        private void TouchpadActionPadPropViewModel_SelectedPadModeIndexChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.PAD_MODE))
            {
                action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.PAD_MODE);
            }

            HighlightPadModeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ChangeStickPadMode(object sender, EventArgs e)
        {
            action.CurrentMode = padModeItems[selectedPadModeIndex].DPadMode;

            ShowCardinalPadChanged?.Invoke(this, EventArgs.Empty);
            ShowDiagonalPadChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TouchpadActionPadPropViewModel_DeadZoneChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.DEAD_ZONE))
            {
                action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.DEAD_ZONE);
            }

            HighlightDeadZoneChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TouchpadActionPadPropViewModel_DiagonalRangeChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.DIAGONAL_RANGE))
            {
                action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.DIAGONAL_RANGE);
            }

            HighlightDiagonalRangeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TouchpadActionPadPropViewModel_RequiresClickChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.REQUIRES_CLICK))
            {
                action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.REQUIRES_CLICK);
            }

            HighlightRequiresClickChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TouchpadActionPadPropViewModel_NameChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TouchpadActionPad.PropertyKeyStrings.NAME))
            {
                action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.NAME);
            }

            HighlightNameChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PrepareModel()
        {
            padModeItems.AddRange(new PadModeItem[]
            {
                new PadModeItem("Standard", TouchpadActionPad.DPadMode.Standard),
                new PadModeItem("Eight Way", TouchpadActionPad.DPadMode.EightWay),
                new PadModeItem("Four Way Cardinal", TouchpadActionPad.DPadMode.FourWayCardinal),
                new PadModeItem("Four Way Diagonal", TouchpadActionPad.DPadMode.FourWayDiagonal),
            });

            int index = padModeItems.FindIndex((item) => item.DPadMode == action.CurrentMode);
            if (index >= 0)
            {
                selectedPadModeIndex = index;
            }
        }
    }

    public class PadModeItem
    {
        private string displayName;
        public string DisplayName
        {
            get => displayName;
        }

        private TouchpadActionPad.DPadMode dpadMode = TouchpadActionPad.DPadMode.Standard;
        public TouchpadActionPad.DPadMode DPadMode
        {
            get => dpadMode;
            set
            {
                if (dpadMode == value) return;
                dpadMode = value;
                DPadModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DPadModeChanged;

        public PadModeItem(string displayName, TouchpadActionPad.DPadMode dpadMode)
        {
            this.displayName = displayName;
            this.dpadMode = dpadMode;
        }
    }
}
