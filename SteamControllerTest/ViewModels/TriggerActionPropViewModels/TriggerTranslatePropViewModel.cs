using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.TriggerActions;

namespace SteamControllerTest.ViewModels.TriggerActionPropViewModels
{
    public class TriggerTranslatePropViewModel
    {
        private Mapper mapper;
        public Mapper Mapper
        {
            get => mapper;
        }

        private TriggerTranslate action;
        public TriggerTranslate Action
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

        public TriggerTranslatePropViewModel(Mapper mapper, TriggerMapAction action)
        {
            this.mapper = mapper;
            this.action = action as TriggerTranslate;

            PrepareModel();

            NameChanged += TriggerTranslatePropViewModel_NameChanged;
        }

        private void TriggerTranslatePropViewModel_NameChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(TriggerTranslate.PropertyKeyStrings.NAME))
            {
                action.ChangedProperties.Add(TriggerTranslate.PropertyKeyStrings.NAME);
            }

            HighlightNameChanged?.Invoke(this, EventArgs.Empty);
        }

        public void PrepareModel()
        {

        }
    }
}
