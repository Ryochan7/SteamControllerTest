﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ViewModels
{
    public class NormalPressFuncPropViewModel
    {
        private Mapper mapper;
        private ButtonAction action;
        private NormalPressFunc func;

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

        public bool ToggleEnabled
        {
            get => func.toggleEnabled;
            set
            {
                func.toggleEnabled = value;
            }
        }

        public NormalPressFuncPropViewModel(Mapper mapper, ButtonAction action,
            NormalPressFunc func)
        {
            this.mapper = mapper;
            this.action = action;
            this.func = func;
        }
    }
}
