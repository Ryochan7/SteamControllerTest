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
    public class TouchpadActionPadPropViewModel
    {
        private Mapper mapper;
        public Mapper Napper
        {
            get => mapper;
        }
        
        private TouchpadActionPad action;
        public TouchpadActionPad Action
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

        public TouchpadActionPadPropViewModel(Mapper mapper, TouchpadMapAction action)
        {
            this.mapper = mapper;
            this.action = action as TouchpadActionPad;
        }
    }
}
