using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.ButtonActions;

namespace SteamControllerTest.ViewModels
{
    public class ButtonFuncEditViewModel
    {
        private Mapper mapper;
        public Mapper Mapper
        {
            get => mapper;
        }

        private ButtonMapAction action;
        public ButtonMapAction Action
        {
            get => action;
        }

        private UserControl displayControl;
        public UserControl DisplayControl
        {
            get => displayControl;
            set
            {
                displayControl = value;
                DisplayControlChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DisplayControlChanged;

        private bool isTransformOutputVisible;
        public bool IsTransformOutputVisible
        {
            get => isTransformOutputVisible;
            set
            {
                if (isTransformOutputVisible == value) return;
                isTransformOutputVisible = value;
                IsTransformOutputVisibleChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler IsTransformOutputVisibleChanged;

        private int selectedTransformIndex = 0;
        public int SelectedTransformIndex
        {
            get => selectedTransformIndex;
            set
            {
                selectedTransformIndex = value;
                SelectedTransformIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedTransformIndexChanged;

        private bool topTransformPanelVisible = true;
        public bool TopTransformPanelVisible
        {
            get => topTransformPanelVisible;
            set
            {
                if (topTransformPanelVisible == value) return;
                topTransformPanelVisible = value;
                TopTransformPanelVisibleChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TopTransformPanelVisibleChanged;

        public ButtonFuncEditViewModel(Mapper mapper, ButtonMapAction action)
        {
            this.mapper = mapper;
            this.action = action;
        }

        public ButtonMapAction PrepareNewAction(int ind)
        {
            ButtonMapAction result = null;
            switch(ind)
            {
                case 0:
                    result = new ButtonNoAction();
                    break;
                case 1:
                    result = new ButtonAction(
                        new NormalPressFunc(
                            new MapperUtil.OutputActionData(MapperUtil.OutputActionData.ActionType.Empty, 0)));
                    break;
                default:
                    break;
            }

            return result;
        }

        public void UpdateAction(ButtonMapAction action)
        {
            if (this.action.Id == MapAction.DEFAULT_UNBOUND_ID)
            {
                // Need to create new ID for action
                action.Id =
                    mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.FindNextAvailableId();
            }
            else
            {
                // Can re-use existing ID
                action.Id = this.action.Id;
            }

            this.action = action;

        }

    }

    public class ButtonActionViewModel
    {
        private Mapper mapper;
        public Mapper Mapper => mapper;
        private ButtonAction action;
        public ButtonAction Action => action;

        private UserControl displayControl;
        public UserControl DisplayControl
        {
            get => displayControl;
            set
            {
                displayControl = value;
                DisplayControlChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DisplayControlChanged;

        public ButtonActionViewModel(Mapper mapper, ButtonMapAction action)
        {
            this.mapper = mapper;
            this.action = action as ButtonAction;
        }
    }

    public class ButtonNoActionViewModel
    {
        private Mapper mapper;
        public Mapper Mapper => mapper;
        private ButtonNoAction action;
        public ButtonNoAction Action => action;

        private UserControl displayControl;
        public UserControl DisplayControl
        {
            get => displayControl;
            set
            {
                displayControl = value;
                DisplayControlChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DisplayControlChanged;

        public ButtonNoActionViewModel(Mapper mapper, ButtonMapAction action)
        {
            this.mapper = mapper;
            this.action = action as ButtonNoAction;
        }
    }
}
