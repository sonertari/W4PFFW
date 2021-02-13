/*
 * Copyright (C) 2017-2021 Soner Tari
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
    public partial class InfoQueues : InfoBase
    {
        private string mQueuesInfo;

        override protected void init()
        {
            InitializeComponent();
        }

        override public void SaveState()
        {
            base.SaveState();

            cache = new InfoQueuesCache();

            (cache as InfoQueuesCache).mQueuesInfo = mQueuesInfo;

            Main.self.cache["InfoQueues"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("InfoQueues"))
            {
                cache = Main.self.cache["InfoQueues"] as InfoQueuesCache;

                mQueuesInfo = (cache as InfoQueuesCache).mQueuesInfo;

                updateView();

                return true;
            }
            return false;
        }

        override protected void fetch()
        {
            mQueuesInfo = Main.controller.execute("pf", "GetPfQueueInfo").output;

            var strReloadRate = Main.controller.execute("pf", "GetReloadRate").output;

            int timeout = int.Parse(strReloadRate);
            refreshTimeout = timeout < 10 ? 10 : timeout;
        }

        override protected void updateView()
        {
            var jsonArr = JsonConvert.DeserializeObject<JArray>(mQueuesInfo);
            queuesDataGrid.ItemsSource = Utils.jsonToStringArray(jsonArr, new List<string> { "name", "pkts", "bytes", "droppedPkts", "droppedBytes", "length" }, false);
        }
    }

    public class InfoQueuesCache : Cache
    {
        public string mQueuesInfo;
    }
}
