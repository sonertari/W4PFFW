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
    public partial class InfoRules : InfoBase
    {
        private string mRulesInfo;

        override protected void init()
        {
            InitializeComponent();
        }

        override public void SaveState()
        {
            base.SaveState();

            cache = new InfoRulesCache();

            (cache as InfoRulesCache).mRulesInfo = mRulesInfo;

            Main.self.cache["InfoRules"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("InfoRules"))
            {
                cache = Main.self.cache["InfoRules"] as InfoRulesCache;

                mRulesInfo = (cache as InfoRulesCache).mRulesInfo;

                updateView();

                return true;
            }
            return false;
        }

        override protected void fetch()
        {
            mRulesInfo = Main.controller.execute("pf", "GetPfRulesInfo").output;

            var strReloadRate = Main.controller.execute("pf", "GetReloadRate").output;

            int timeout = int.Parse(strReloadRate);
            refreshTimeout = timeout < 10 ? 10 : timeout;
        }

        override protected void updateView()
        {
            var jsonArr = JsonConvert.DeserializeObject<JArray>(mRulesInfo);
            rulesDataGrid.ItemsSource = Utils.jsonToStringArray(jsonArr, new List<string> { "number", "evaluations", "packets", "bytes", "states", "stateCreations", "rule", "inserted" }, false);
        }
    }

    public class InfoRulesCache : Cache
    {
        public string mRulesInfo;
    }
}
