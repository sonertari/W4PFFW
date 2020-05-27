/*
 * Copyright (C) 2017-2020 Soner Tari
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
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PFFW
{
    public partial class StatsDaily : StatsBase
    {
        override protected void init()
        {
            InitializeComponent();

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

            updateDateTimeText();
        }

        override public void SaveState()
        {
            cache = new StatsDailyCache();

            (cache as StatsDailyCache).isAllMonths = isAllMonths();
            (cache as StatsDailyCache).isAllDays = isAllDays();
            cache.isDailyChart = isDailyChart();

            base.SaveState();
            logFilePicker.SaveState(cache);

            Main.self.cache["StatsDaily"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("StatsDaily"))
            {
                cache = Main.self.cache["StatsDaily"] as StatsDailyCache;

                btnAllMonths.Content = (cache as StatsDailyCache).isAllMonths ? "All Months" : "Single Month";
                btnAllDays.Content = (cache as StatsDailyCache).isAllDays ? "All Days" : "Single Day";
                btnDaily.Content = cache.isDailyChart ? "Daily" : "Hourly";

                restoreStateBase();
                logFilePicker.restoreState(cache);

                updateDateTimeText();
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
            else if (sender.Equals(btnAllMonths))
            {
                setAllMonths(isAllMonths() ? "Single Month" : "All Months");
            }
            else if (sender.Equals(btnAllDays))
            {
                setAllDays(isAllDays() ? "Single Day" : "All Days");
            }
            else if (sender.Equals(btnDefaults))
            {
                setDefaults();
            }

            updateDateTimeText();
        }

        override protected bool isDailyChart()
        {
            return btnDaily.Content.Equals("Daily");
        }

        override protected void refresh()
        {
            getSelectedDateTime();
            fetchStats();
            logFilePicker.refresh();
            updateStats();
        }

        override protected void fetch()
        {
            var logfile = logFilePicker.selectLogFile();

            var tuple = logFilePicker.getLogStartDate(month, day);
            month = tuple.Item1;
            day = tuple.Item2;

            var m = isAllMonths() ? "" : month;
            var d = isAllDays() ? "" : day;

            var jsonDate = JsonConvert.SerializeObject(new Dictionary<string, string> { { "Month", m }, { "Day", d } });
            var collect = isDailyChart() ? "" : "COLLECT";

            var strStats = Main.controller.execute("pf", "GetStats", logfile, jsonDate, collect).output;
            jsonStats = JsonConvert.DeserializeObject<JObject>(strStats);

            logFilePicker.fetch();
        }

        void getSelectedDateTime()
        {
            if (datePicker.SelectedDate != null)
            {
                month = datePicker.SelectedDate.Value.Month.ToString().PadLeft(2, '0');
                day = datePicker.SelectedDate.Value.Day.ToString().PadLeft(2, '0');
            }
        }

        void updateDateTimeText()
        {
            if (isAllMonths() && isAllDays())
            {
                datePicker.Text = null;
            }
            else
            {
                datePicker.Text = day + "." + month;
            }
        }

        void setChartType(string type)
        {
            if (!isAllMonths() && !isAllDays())
            {
                btnDaily.Content = "Hourly";
            }
            else
            {
                btnDaily.Content = type;
            }
        }

        private bool isAllMonths()
        {
            return btnAllMonths.Content.Equals("All Months");
        }

        private void setAllMonths(string type)
        {
            btnAllMonths.Content = type;

            if (!isAllMonths() && !isAllDays())
            {
                setChartType("Hourly");
            }

            if (isAllMonths())
            {
                setAllDays("All Days");
            }
        }

        private bool isAllDays()
        {
            return btnAllDays.Content.Equals("All Days");
        }

        private void setAllDays(string type)
        {
            btnAllDays.Content = type;

            if (!isAllMonths() && !isAllDays())
            {
                setChartType("Hourly");
            }

            if (!isAllDays())
            {
                setAllMonths("Single Month");
            }
        }

        private void setDefaults()
        {
            setAllMonths("All Months");
            setAllDays("All Days");
            setChartType("Daily");
        }
    }

    public class StatsDailyCache : StatsCache
    {
        public bool isAllMonths;
        public bool isAllDays;
    }
}
