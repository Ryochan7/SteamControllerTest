using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.ButtonActions;

namespace SteamControllerTest.ViewModels
{
    public class DistanceFuncPropViewModel
    {
        private Mapper mapper;
        private ButtonAction action;
        private DistanceFunc func;

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

        public double Distance
        {
            get => func.distance;
            set
            {
                func.distance = Math.Clamp(value, 0.0, 1.0);
                DistanceChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DistanceChanged;

        public DistanceFuncPropViewModel(Mapper mapper, ButtonAction action,
            DistanceFunc func)
        {
            this.mapper = mapper;
            this.action = action;
            this.func = func;
        }
    }
}
