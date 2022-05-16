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
    public class TouchpadMousePropViewModel
    {
        private Mapper mapper;
        public Mapper Mapper => mapper;

        private TouchpadMouse action;
        public TouchpadMouse Action => action;

        public string Name
        {
            get => action.Name;
            set
            {
                action.Name = value;
            }
        }

        public TouchpadMousePropViewModel(Mapper mapper, TouchpadMapAction action)
        {
            this.mapper = mapper;
            this.action = action as TouchpadMouse;
        }
    }
}
