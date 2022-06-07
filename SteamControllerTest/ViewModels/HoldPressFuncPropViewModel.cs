using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ViewModels
{
    public class HoldPressFuncPropViewModel
    {
        private Mapper mapper;
        private ButtonAction action;
        private HoldPressFunc func;

        public string Name
        {
            get => func.Name;
            set
            {
                func.Name = value;
            }
        }

        public string DisplayBind
        {
            get
            {
                string result = "";
                result = func.DescribeOutputActions(mapper);
                return result;
            }
        }

        public int HoldMs
        {
            get => func.DurationMs;
            set
            {
                func.DurationMs = value;
            }
        }

        public HoldPressFuncPropViewModel(Mapper mapper, ButtonAction action,
            HoldPressFunc func)
        {
            this.mapper = mapper;
            this.action = action;
            this.func = func;
        }
    }
}
