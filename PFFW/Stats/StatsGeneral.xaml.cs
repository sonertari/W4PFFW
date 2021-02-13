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

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PFFW
{
    public partial class StatsGeneral : StatsBase
    {
        private Dictionary<string, KeyValuePair<TextBlock, TextBlock>> generalStatsTables;

        private JObject jsonBriefStats;
        private JObject jsonGeneralStats;

        override protected void init()
        {
            InitializeComponent();

            generalStatsTables = new Dictionary<string, KeyValuePair<TextBlock, TextBlock>> {
                { "SrcIP",  new KeyValuePair<TextBlock, TextBlock>(generalLists.srcAddrs, generalLists.srcAddrsCounts) },
                { "DstIP",  new KeyValuePair<TextBlock, TextBlock>(generalLists.dstAddrs, generalLists.dstAddrsCounts) },
                { "DPort",  new KeyValuePair<TextBlock, TextBlock>(generalLists.dstPorts, generalLists.dstPortsCounts) },
                { "Type",  new KeyValuePair<TextBlock, TextBlock>(generalLists.pktTypes, generalLists.pktTypesCounts) },
                };

            chartDefs = new Dictionary<string, ChartDef> {
                { "Total", new ChartDef(Colors.Blue, new double[0], new string[0], totalRowChart, totalColumnChart, "0", totalTotal) },
                { "Pass", new ChartDef(Colors.Green, new double[0], new string[0], passRowChart, passColumnChart, "0", passTotal) },
                { "Block", new ChartDef(Colors.Red, new double[0], new string[0], blockRowChart, blockColumnChart, "0", blockTotal) },
                { "Match", new ChartDef(Colors.Yellow, new double[0], new string[0], matchRowChart, matchColumnChart, "0", matchTotal) },
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
        }

        override public void SaveState()
        {
            cache = new StatsGeneralCache();
            cache.isDailyChart = isDailyChart();
            (cache as StatsGeneralCache).jsonBriefStats = jsonBriefStats;
            (cache as StatsGeneralCache).jsonGeneralStats = jsonGeneralStats;

            base.SaveState();
            logFilePicker.SaveState(cache);

            Main.self.cache["StatsGeneral"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("StatsGeneral"))
            {
                cache = Main.self.cache["StatsGeneral"] as StatsGeneralCache;

                btnDaily.Content = cache.isDailyChart ? "Daily" : "Hourly";
                jsonBriefStats = (cache as StatsGeneralCache).jsonBriefStats;
                jsonGeneralStats = (cache as StatsGeneralCache).jsonGeneralStats;

                restoreStateBase();
                logFilePicker.restoreState(cache);

                updateGeneralStatsTable();
                updateRequestsByDateTable();
                updateGeneralStats();

                return true;
            }
            return false;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(btnRefresh))
            {
                refresh();
            }
            else if (sender.Equals(btnDaily))
            {
                setChartType(isDailyChart() ? "Hourly" : "Daily");
            }
        }

        override protected bool isDailyChart()
        {
            return btnDaily.Content.Equals("Daily");
        }

        override protected void refresh()
        {
            fetchStats();

            logFilePicker.refresh();

            updateStats();
        }

        override protected void fetch()
        {
            var logfile = logFilePicker.selectLogFile();

            // Whether to collect hourly stats too
            var collect = isDailyChart() ? "" : "COLLECT";

            var strStats = Main.controller.execute("pf", "GetAllStats", logfile, collect).output;
            var jsonAllStats = JsonConvert.DeserializeObject<JObject>(strStats);
            jsonBriefStats = JsonConvert.DeserializeObject<JObject>(jsonAllStats["briefstats"].ToString()) as JObject;
            jsonStats = JsonConvert.DeserializeObject<JObject>(jsonAllStats["stats"].ToString()) as JObject;

            var strGeneralStats = Main.controller.execute("pf", "GetProcStatLines", logfile).output;
            jsonGeneralStats = JsonConvert.DeserializeObject<JObject>(strGeneralStats);

            logFilePicker.fetch();
        }

        void setChartType(string type)
        {
            btnDaily.Content = type;
        }

        override protected void updateStats()
        {
            updateGeneralStatsTable();
            updateRequestsByDateTable();
            updateGeneralStats();

            base.updateStats();
        }

        private void updateGeneralStatsTable()
        {
            var c = "";
            var i = "";

            var it = jsonGeneralStats.GetEnumerator();
            while (it.MoveNext())
            {
                c += jsonGeneralStats[it.Current.Key] + "\n";
                i += it.Current.Key + "\n";
            }

            generalStatsCounts.Text = c.TrimEnd();
            generalStats.Text = i.TrimEnd();
        }

        private void updateRequestsByDateTable()
        {
            var list = new Dictionary<string, int>();

            var it = (jsonBriefStats["Date"] as JObject).GetEnumerator();
            while (it.MoveNext())
            {
                list[it.Current.Key] = int.Parse(jsonBriefStats["Date"][it.Current.Key].ToString());
            }

            var c = "";
            var i = "";
            foreach (var kvp in list.OrderByDescending(kvp => kvp.Value))
            {
                c += kvp.Value + "\n";
                i += kvp.Key + "\n";
            }

            requestsByDateCounts.Text = c.TrimEnd();
            requestsByDate.Text = i.TrimEnd();
        }

        private void updateGeneralStats()
        {
            foreach (string sk in generalStatsTables.Keys)
            {
                var list = new Dictionary<string, int>();

                var it = (jsonBriefStats[sk] as JObject).GetEnumerator();
                while (it.MoveNext())
                {
                    list[it.Current.Key] = int.Parse(jsonBriefStats[sk][it.Current.Key].ToString());
                }

                var c = "";
                var i = "";
                foreach (var kvp in list.OrderByDescending(kvp => kvp.Value))
                {
                    c += kvp.Value + "\n";
                    i += kvp.Key + "\n";
                }

                generalStatsTables[sk].Value.Text = c.TrimEnd();
                generalStatsTables[sk].Key.Text = i.TrimEnd();
            }
        }

    }

    public class StatsGeneralCache : StatsCache
    {
        public JObject jsonBriefStats;
        public JObject jsonGeneralStats;
    }
}
