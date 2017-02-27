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
using System.Windows.Media.Imaging;

namespace PFFW
{
    /// <summary>
    /// DHCP and DNS service information.
    /// </summary>
    public partial class InfoHosts : InfoBase
    {
        private int mDhcpdStatus;
        private string mDhcpdInfo;

        private int mNamedStatus;
        private string mNamedInfo;

        private string mArpTableInfo;
        private string mLeasesInfo;

        override protected void init()
        {
            InitializeComponent();
        }

        override public void SaveState()
        {
            base.SaveState();

            cache = new InfoHostsCache();

            (cache as InfoHostsCache).mDhcpdStatus = mDhcpdStatus;
            (cache as InfoHostsCache).mDhcpdInfo = mDhcpdInfo;

            (cache as InfoHostsCache).mNamedStatus = mNamedStatus;
            (cache as InfoHostsCache).mNamedInfo = mNamedInfo;

            (cache as InfoHostsCache).mArpTableInfo = mArpTableInfo;
            (cache as InfoHostsCache).mLeasesInfo = mLeasesInfo;

            Main.self.cache["InfoHosts"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("InfoHosts"))
            {
                cache = Main.self.cache["InfoHosts"] as InfoHostsCache;

                mDhcpdStatus = (cache as InfoHostsCache).mDhcpdStatus;
                mDhcpdInfo = (cache as InfoHostsCache).mDhcpdInfo;

                mNamedStatus = (cache as InfoHostsCache).mNamedStatus;
                mNamedInfo = (cache as InfoHostsCache).mNamedInfo;

                mArpTableInfo = (cache as InfoHostsCache).mArpTableInfo;
                mLeasesInfo = (cache as InfoHostsCache).mLeasesInfo;

                updateView();

                return true;
            }
            return false;
        }

        override protected void fetch()
        {
            mDhcpdStatus = Main.controller.execute("dhcpd", "IsRunning").status;
            mDhcpdInfo = Main.controller.execute("dhcpd", "GetProcList").output;

            mNamedStatus = Main.controller.execute("named", "IsRunning").status;
            mNamedInfo = Main.controller.execute("named", "GetProcList").output;

            mArpTableInfo = Main.controller.execute("dhcpd", "GetArpTable").output;
            mLeasesInfo = Main.controller.execute("dhcpd", "GetLeases").output;

            var strReloadRate = Main.controller.execute("pf", "GetReloadRate").output;

            int timeout = int.Parse(strReloadRate);
            refreshTimeout = timeout < 10 ? 10 : timeout;
        }

        override protected void updateView()
        {
            dhcpdStatusImage.Source = Resources[mDhcpdStatus == 0 ? "run" : "stop"] as BitmapImage;
            dhcpdStatus.Content = mDhcpdStatus == 0 ? "DHCP Server is running" : "DHCP Server is not running";

            var jsonArr = JsonConvert.DeserializeObject<JArray>(mDhcpdInfo);
            dhcpdDataGrid.ItemsSource = JsonConvert.DeserializeObject<string[][]>(jsonArr.ToString());

            namedStatusImage.Source = Resources[mNamedStatus == 0 ? "run" : "stop"] as BitmapImage;
            namedStatus.Content = mNamedStatus == 0 ? "DNS Server is running" : "DNS Server is not running";

            jsonArr = JsonConvert.DeserializeObject<JArray>(mNamedInfo);
            namedDataGrid.ItemsSource = JsonConvert.DeserializeObject<string[][]>(jsonArr.ToString());

            jsonArr = JsonConvert.DeserializeObject<JArray>(mArpTableInfo);
            arpTableDataGrid.ItemsSource = Utils.jsonToStringArray(jsonArr, new List<string> { "IP", "MAC", "Interface", "Expire" });

            jsonArr = JsonConvert.DeserializeObject<JArray>(mLeasesInfo);
            leasesDataGrid.ItemsSource = Utils.jsonToStringArray(jsonArr, new List<string> { "IP", "Start", "End", "MAC", "Host", "Status" });
        }
    }

    public class InfoHostsCache : Cache
    {
        public int mDhcpdStatus;
        public string mDhcpdInfo;
        public int mNamedStatus;
        public string mNamedInfo;
        public string mArpTableInfo;
        public string mLeasesInfo;
    }
}
