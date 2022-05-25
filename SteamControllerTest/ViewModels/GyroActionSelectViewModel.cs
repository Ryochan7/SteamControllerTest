using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.GyroActions;

namespace SteamControllerTest.ViewModels
{
    public class GyroActionSelectViewModel
    {
        private Mapper mapper;
        private GyroMapAction action;

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

        public GyroActionSelectViewModel(Mapper mapper, GyroMapAction action)
        {
            this.mapper = mapper;
            this.action = action;
        }

        public void PrepareView()
        {
            switch (action)
            {
                case GyroNoMapAction:
                    selectedIndex = 0;
                    break;
                case GyroMouse:
                    selectedIndex = 1;
                    break;
                case GyroMouseJoystick:
                    selectedIndex = 2;
                    break;
                default:
                    selectedIndex = -1;
                    break;
            }
        }
    }
}
