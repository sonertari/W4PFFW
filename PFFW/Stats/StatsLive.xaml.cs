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

using System;
using System.Collections.Generic;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Timers;

namespace PFFW
{
    public partial class StatsLive : StatsHourlyBase
    {
        private string logFile = "";

        public string minute = "00";

        Timer timer;
        int refreshTimeout = 10;
        bool timerEventRunning = false;

        override protected void init()
        {
            InitializeComponent();

            chartDefs = new Dictionary<string, ChartDef> {
                { "Total", new ChartDef(Colors.Blue, new double[0], new string[0], null, totalColumnChart, "0", totalTotal) },
                { "Pass", new ChartDef(Colors.Green, new double[0], new string[0], null, passColumnChart, "0", passTotal) },
                { "Block", new ChartDef(Colors.Red, new double[0], new string[0], null, blockColumnChart, "0", blockTotal) },
                { "Match", new ChartDef(Colors.Yellow, new double[0], new string[0], null, matchColumnChart, "0", matchTotal) },
                };

            listDefs = new Dictionary<string, Dictionary<string, ListDef>> {
                { "Total", new Dictionary<string, ListDef> {
                    { "SrcIP", new ListDef(new Dictionary<string, int>(), totalLists.srcAddrs, totalLists.srcAddrsCounts) },
                    { "DstIP", new ListDef(new Dictionary<string, int>(), totalLists.dstAddrs, totalLists.dstAddrsCounts) },
                    { "DPort", new ListDef(new Dictionary<string, int>(), totalLists.dstPorts, totalLists.dstPortsCounts) },
                    { "Type", new ListDef(new Dictionary<string, int>(), totalLists.pktTypes, totalLists.pktTypesCounts) }}},
                { "Pass", new Dictionary<string, ListDef> {
                    { "SrcIP", new ListDef(new Dictionary<string, int>(), passLists.srcAddrs, passLists.srcAddrsCounts) },
                    { "DstIP", new ListDef(new Dictionary<string, int>(), passLists.dstAddrs, passLists.dstAddrsCounts) },
                    { "DPort", new ListDef(new Dictionary<string, int>(), passLists.dstPorts, passLists.dstPortsCounts) },
                    { "Type", new ListDef(new Dictionary<string, int>(), passLists.pktTypes, passLists.pktTypesCounts) }}},
                { "Block", new Dictionary<string, ListDef> {
                    { "SrcIP", new ListDef(new Dictionary<string, int>(), blockLists.srcAddrs, blockLists.srcAddrsCounts) },
                    { "DstIP", new ListDef(new Dictionary<string, int>(), blockLists.dstAddrs, blockLists.dstAddrsCounts) },
                    { "DPort", new ListDef(new Dictionary<string, int>(), blockLists.dstPorts, blockLists.dstPortsCounts) },
                    { "Type", new ListDef(new Dictionary<string, int>(), blockLists.pktTypes, blockLists.pktTypesCounts) }}},
                { "Match", new Dictionary<string, ListDef> {
                    { "SrcIP", new ListDef(new Dictionary<string, int>(), matchLists.srcAddrs, matchLists.srcAddrsCounts) },
                    { "DstIP", new ListDef(new Dictionary<string, int>(), matchLists.dstAddrs, matchLists.dstAddrsCounts) },
                    { "DPort", new ListDef(new Dictionary<string, int>(), matchLists.dstPorts, matchLists.dstPortsCounts) },
                    { "Type", new ListDef(new Dictionary<string, int>(), matchLists.pktTypes, matchLists.pktTypesCounts) }}},
                };

            // TODO: Do not forget to make this * 1000
            timer = new Timer(refreshTimeout * 1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        delegate void RefreshDelegate();

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!timerEventRunning)
            {
                timerEventRunning = true;
                try
                {
                    if (!Dispatcher.CheckAccess())
                    {
                        Dispatcher.Invoke(new RefreshDelegate(refresh));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
                finally
                {
                    timerEventRunning = false;

                }
            }
        }

        override public void SaveState()
        {
            timer.Stop();

            cache = new StatsLiveCache();

            (cache as StatsLiveCache).hour = hour;
            (cache as StatsLiveCache).minute = minute;

            base.SaveState();
            cache.logFile = logFile;

            Main.self.cache["StatsLive"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("StatsLive"))
            {
                cache = Main.self.cache["StatsLive"] as StatsLiveCache;

                hour = (cache as StatsLiveCache).hour;
                minute = (cache as StatsLiveCache).minute;

                restoreStateBase();
                logFile = cache.logFile;

                updateDateTimeText();
                return true;
            }
            return false;
        }

        override protected void refresh()
        {
            fetchStats();
            updateDateTimeText();

            updateJsonVars();
            updateStats();
        }

        void updateDateTimeText()
        {
            dateLabel.Content = "Date: " + Utils.monthNames[month] + " " + day + ", " + hour + ":" + minute;
        }

        override protected void fetch()
        {
            logFile = Main.controller.execute("pf", "GetDefaultLogFile").output;

            var strDateTime = Main.controller.execute("pf", "GetDateTime").output;
            var jsonDateTime = JsonConvert.DeserializeObject<JObject>(strDateTime);

            month = jsonDateTime["Month"].ToString().PadLeft(2, '0');
            day = jsonDateTime["Day"].ToString().PadLeft(2, '0');
            hour = jsonDateTime["Hour"].ToString().PadLeft(2, '0');
            minute = jsonDateTime["Minute"].ToString().PadLeft(2, '0');

            var jsonDate = JsonConvert.SerializeObject(new Dictionary<string, string> { { "Month", month }, { "Day", day }, { "Hour", hour } });

            var strStats = Main.controller.execute("pf", "GetStats", logFile, jsonDate, "COLLECT").output;
            jsonStats = JsonConvert.DeserializeObject<JObject>(strStats);

            var strReloadRate = Main.controller.execute("pf", "GetReloadRate").output;

            int timeout = int.Parse(strReloadRate);
            refreshTimeout = timeout < 10 ? 10 : timeout;
        }
    }

    public class StatsLiveCache : StatsHourlyCache
    {
        public string minute;
    }
}
