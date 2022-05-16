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
    public class TouchpadMouseJoystickPropViewModel
    {
        private Mapper mapper;
        public Mapper Mapper
        {
            get => mapper;
        }

        private TouchpadMouseJoystick action;
        public TouchpadMouseJoystick Action
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

        public TouchpadMouseJoystickPropViewModel(Mapper mapper,
            TouchpadMapAction action)
        {
            this.mapper = mapper;
            this.action = action as TouchpadMouseJoystick;

            outputStickItems.AddRange(new OutputStickSelectionItem[]
            {
                new OutputStickSelectionItem("Unbound", StickActionCodes.Empty),
                new OutputStickSelectionItem("Left Stick", StickActionCodes.X360_LS),
                new OutputStickSelectionItem("Right Stick", StickActionCodes.X360_RS),
            });

            OutputStickIndexChanged += TouchpadMouseJoystickPropViewModel_OutputStickIndexChanged;
        }

        private void TouchpadMouseJoystickPropViewModel_OutputStickIndexChanged(object sender, EventArgs e)
        {
            OutputStickSelectionItem item = outputStickItems[outputStickIndex];
            mapper.QueueEvent(() =>
            {
                action.MStickParams.OutputStick = item.Code;
            });
        }

        private void PrepareModel()
        {
            switch (action.MStickParams.OutputStick)
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
}
