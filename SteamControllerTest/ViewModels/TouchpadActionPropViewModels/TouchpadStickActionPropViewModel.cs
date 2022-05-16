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
    public class TouchpadStickActionPropViewModel
    {
        private Mapper mapper;
        public Mapper Mapper
        {
            get => mapper;
        }

        private TouchpadStickAction action;
        public TouchpadStickAction Action
        {
            get => action;
        }

        public string Name
        {
            get => action.Name;
            set
            {
                action.Name = value;
            }
        }

        private List<OutputStickSelectionItem> outputStickItems =
            new List<OutputStickSelectionItem>();
        public List<OutputStickSelectionItem> OutputStickItems => outputStickItems;

        private int outputStickIndex = -1;
        public int OutputStickIndex
        {
            get => outputStickIndex;
            set
            {
                outputStickIndex = value;
                OutputStickIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler OutputStickIndexChanged;

        public string DeadZone
        {
            get => action.DeadMod.DeadZone.ToString();
            set
            {
                if (double.TryParse(value, out double temp))
                {
                    action.DeadMod.DeadZone = Math.Clamp(temp, 0.0, 1.0);
                }
            }
        }

        public string AntiDeadZone
        {
            get => action.DeadMod.AntiDeadZone.ToString();
            set
            {
                if (double.TryParse(value, out double temp))
                {
                    action.DeadMod.AntiDeadZone = Math.Clamp(temp, 0.0, 1.0);
                }
            }
        }

        public string MaxZone
        {
            get => action.DeadMod.MaxZone.ToString();
            set
            {
                if (double.TryParse(value, out double temp))
                {
                    action.DeadMod.MaxZone = Math.Clamp(temp, 0.0, 1.0);
                }
            }
        }

        public TouchpadStickActionPropViewModel(Mapper mapper,
            TouchpadMapAction action)
        {
            this.mapper = mapper;
            this.action = action as TouchpadStickAction;

            outputStickItems.AddRange(new OutputStickSelectionItem[]
            {
                new OutputStickSelectionItem("Unbound", StickActionCodes.Empty),
                new OutputStickSelectionItem("Left Stick", StickActionCodes.X360_LS),
                new OutputStickSelectionItem("Right Stick", StickActionCodes.X360_RS),
            });

            PrepareModel();

            OutputStickIndexChanged += TouchpadStickActionPropViewModel_OutputStickIndexChanged;
        }

        private void TouchpadStickActionPropViewModel_OutputStickIndexChanged(object sender, EventArgs e)
        {
            OutputStickSelectionItem item = outputStickItems[outputStickIndex];
            mapper.QueueEvent(() =>
            {
                action.OutputAction.StickCode = item.Code;
            });
        }

        private void PrepareModel()
        {
            switch(action.OutputAction.StickCode)
            {
                case StickActionCodes.Empty:
                    outputStickIndex = 0;
                    break;
                case StickActionCodes.X360_LS:
                    outputStickIndex = 1;
                    break;
                case StickActionCodes.X360_RS:
                    outputStickIndex = 2;
                    break;
                default:
                    break;
            }
        }
    }

    public class OutputStickSelectionItem
    {
        private string displayName;
        public string DisplayName
        {
            get => displayName;
        }

        private StickActionCodes code;
        public StickActionCodes Code => code;

        public OutputStickSelectionItem(string displayName, StickActionCodes code)
        {
            this.displayName = displayName;
            this.code = code;
        }
    }
}
