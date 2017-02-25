/*
 * Copyright (C) 2017 Soner Tari
 *
 * This file is part of PFFW.
 *
 * PFFW is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * PFFW is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with PFFW.  If not, see <http://www.gnu.org/licenses/>.
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;

namespace PFFW
{
    /// <summary>
    /// Symon, symux, and system information.
    /// </summary>
    public partial class InfoSystem : InfoBase
    {
        private int mSymonStatus;
        private string mSymonInfo;

        private int mSymuxStatus;
        private string mSymuxInfo;

        private string mSystemInfo;

        override protected void init()
        {
            InitializeComponent();
        }

        override public void SaveState()
        {
            base.SaveState();

            cache = new InfoSystemCache();

            (cache as InfoSystemCache).mSymonStatus = mSymonStatus;
            (cache as InfoSystemCache).mSymonInfo = mSymonInfo;

            (cache as InfoSystemCache).mSymuxStatus = mSymuxStatus;
            (cache as InfoSystemCache).mSymuxInfo = mSymuxInfo;

            (cache as InfoSystemCache).mSystemInfo = mSystemInfo;

            Main.self.cache["InfoSystem"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("InfoSystem"))
            {
                cache = Main.self.cache["InfoSystem"] as InfoSystemCache;

                mSymonStatus = (cache as InfoSystemCache).mSymonStatus;
                mSymonInfo = (cache as InfoSystemCache).mSymonInfo;

                mSymuxStatus = (cache as InfoSystemCache).mSymuxStatus;
                mSymuxInfo = (cache as InfoSystemCache).mSymuxInfo;

                mSystemInfo = (cache as InfoSystemCache).mSystemInfo;

                updateView();

                return true;
            }
            return false;
        }

        override protected void fetch()
        {
            mSymonStatus = Main.controller.execute("symon", "IsRunning").status;
            mSymonInfo = Main.controller.execute("symon", "GetProcList").output;

            mSymuxStatus = Main.controller.execute("symux", "IsRunning").status;
            mSymuxInfo = Main.controller.execute("symux", "GetProcList").output;

            mSystemInfo = Main.controller.execute("system", "GetProcList").output;

            var strReloadRate = Main.controller.execute("pf", "GetReloadRate").output;

            int timeout = int.Parse(strReloadRate);
            refreshTimeout = timeout < 10 ? 10 : timeout;
        }

        override protected void updateView()
        {
            symonStatusImage.Source = Resources[mSymonStatus == 0 ? "run" : "stop"] as BitmapImage;
            symonStatus.Content = mSymonStatus == 0 ? "Symon is running" : "Symon is not running";

            var jsonArr = JsonConvert.DeserializeObject<JArray>(mSymonInfo);
            symonDataGrid.ItemsSource = JsonConvert.DeserializeObject<string[][]>(jsonArr.ToString());

            symuxStatusImage.Source = Resources[mSymuxStatus == 0 ? "run" : "stop"] as BitmapImage;
            symuxStatus.Content = mSymuxStatus == 0 ? "Symux is running" : "Symux is not running";

            jsonArr = JsonConvert.DeserializeObject<JArray>(mSymuxInfo);
            symuxDataGrid.ItemsSource = JsonConvert.DeserializeObject<string[][]>(jsonArr.ToString());

            jsonArr = JsonConvert.DeserializeObject<JArray>(mSystemInfo);
            systemDataGrid.ItemsSource = JsonConvert.DeserializeObject<string[][]>(jsonArr.ToString());
        }
    }

    public class InfoSystemCache : Cache
    {
        public int mSymonStatus;
        public string mSymonInfo;
        public int mSymuxStatus;
        public string mSymuxInfo;
        public string mSystemInfo;
    }
}
