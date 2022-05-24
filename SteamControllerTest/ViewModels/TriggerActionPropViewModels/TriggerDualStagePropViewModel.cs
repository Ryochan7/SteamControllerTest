using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.TriggerActions;

namespace SteamControllerTest.ViewModels.TriggerActionPropViewModels
{
    public class TriggerDualStagePropViewModel
    {
        private Mapper mapper;
        public Mapper Mapper
        {
            get => mapper;
        }

        private TriggerDualStageAction action;
        public TriggerDualStageAction Action
        {
            get => action;
        }

        public string Name
        {
            get => action.Name;
            set
            {
                if (action.Name == value) return;
                action.Name = value;
                NameChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler NameChanged;

        public bool HighlightName
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.NAME);
        }
        public event EventHandler HighlightNameChanged;

        public event EventHandler ActionPropertyChanged;

        public TriggerDualStagePropViewModel(Mapper mapper, TriggerMapAction action)
        {
            this.mapper = mapper;
            this.action = action as TriggerDualStageAction;

            PrepareModel();

            NameChanged += TriggerDualStagePropViewModel_NameChanged;
        }

        private void TriggerDualStagePropViewModel_NameChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TriggerDualStageAction.PropertyKeyStrings.NAME))
            {
                action.ChangedProperties.Add(TriggerDualStageAction.PropertyKeyStrings.NAME);
            }

            HighlightNameChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PrepareModel()
        {
        }
    }
}
