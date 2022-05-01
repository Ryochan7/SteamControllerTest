using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SteamControllerTest
{
    public class AppSettingsStore
    {
        private string configPath;

        public AppSettingsStore()
        {
        }

        public AppSettingsStore(string configPath)
        {
            if (!File.Exists(configPath))
            {
                throw new Exception($"Passed path {configPath} does not exist");
            }

            this.configPath = configPath;
        }

        public void LoadConfig()
        {
            if (string.IsNullOrEmpty(configPath) ||
                !File.Exists(configPath))
            {
                return;
            }
        }

        public void SaveConfig()
        {
            if (string.IsNullOrEmpty(configPath))
            {
                return;
            }


        }
    }
}
