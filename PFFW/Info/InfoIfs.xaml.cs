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
using System.Collections.Generic;

namespace PFFW
{
    /// <summary>
    /// Interface information.
    /// </summary>
    public partial class InfoIfs : InfoBase
    {
        private string mIfsInfo;

        override protected void init()
        {
            InitializeComponent();
        }

        override public void SaveState()
        {
            base.SaveState();

            cache = new InfoIfsCache();

            (cache as InfoIfsCache).mIfsInfo = mIfsInfo;

            Main.self.cache["InfoIfs"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("InfoIfs"))
            {
                cache = Main.self.cache["InfoIfs"] as InfoIfsCache;

                mIfsInfo = (cache as InfoIfsCache).mIfsInfo;

                updateView();

                return true;
            }
            return false;
        }

        override protected void fetch()
        {
            mIfsInfo = Main.controller.execute("pf", "GetPfIfsInfo").output;

            var strReloadRate = Main.controller.execute("pf", "GetReloadRate").output;

            int timeout = int.Parse(strReloadRate);
            refreshTimeout = timeout < 10 ? 10 : timeout;
        }

        override protected void updateView()
        {
            var jsonArr = JsonConvert.DeserializeObject<JArray>(mIfsInfo);
            ifsDataGrid.ItemsSource = Utils.jsonToStringArray(jsonArr, new List<string> { "name", "states", "rules",
                "in4PassPackets", "in4PassBytes", "in4BlockPackets", "in4BlockBytes",
                "out4PassPackets", "out4PassBytes", "out4BlockPackets", "out4BlockBytes",
                "in6PassPackets", "in6PassBytes", "in6BlockPackets", "in6BlockBytes",
                "out6PassPackets", "out6PassBytes", "out6BlockPackets", "out6BlockBytes",
                "cleared" }, false);
        }
    }

    public class InfoIfsCache : Cache
    {
        public string mIfsInfo;
    }
}
