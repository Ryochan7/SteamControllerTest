using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.StickActions;

namespace SteamControllerTest.ViewModels
{
    public class StickActionSelectViewModel
    {
        private Mapper mapper;
        private StickMapAction action;

        private int selectedIndex = -1;
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (selectedIndex == value) return;
                selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedIndexChanged;

        public StickActionSelectViewModel(Mapper mapper, StickMapAction action)
        {
            this.mapper = mapper;
            this.action = action;
        }

        public void PrepareView()
        {
            switch (action)
            {
                case StickNoAction:
                    selectedIndex = 0;
                    break;
                case StickTranslate:
                    selectedIndex = 1;
                    break;
                case StickPadAction:
                    selectedIndex = 2;
                    break;
                case StickMouse:
                    selectedIndex = 3;
                    break;
                case StickCircular:
                    selectedIndex = 4;
                    break;
                default:
                    selectedIndex = -1;
                    break;
            }
        }
    }
}
