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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PFFW
{
    public partial class StatsHourly : StatsHourlyBase
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

            cbHourPicker.ItemsSource = new List<string> { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" };

            updateDateTimeText();
        }

        override public void SaveState()
        {
            cache = new StatsHourlyCache();

            (cache as StatsHourlyCache).hour = hour;

            base.SaveState();

            Main.self.cache["StatsHourly"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("StatsHourly"))
            {
                cache = Main.self.cache["StatsHourly"] as StatsHourlyCache;

                hour = (cache as StatsHourlyCache).hour;

                restoreStateBase();

                updateLogFilePicker();
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

            updateDateTimeText();
        }

        override protected void refresh()
        {
            getSelectedLogFile();
            getSelectedDateTime();

            fetchStats();

            updateLogFileLists();
            updateLogFilePicker();
            // ATTENTION: Log file may change after the log file list is updated
            getSelectedLogFile();
            updateDateTimeText();

            updateJsonVars();
            updateStats();
        }

        // XXX: Code reuse.
        private void updateLogFilePicker()
        {
            // TODO: Make this work?
            //cbLogFilePicker.ItemsSource = logFileOpts;
            cbLogFilePicker.Items.Clear();
            logFileOpts.ForEach(item => cbLogFilePicker.Items.Add(item));
            cbLogFilePicker.Text = selectedLogFileOpt;
        }

        override protected void fetch()
        {
            logFile = Main.controller.execute("pf", "SelectLogFile", logFile).output;

            if (isLogFileChanged())
            {
                var strDate = Main.controller.execute("pf", "GetLogStartDate", logFile).output;

                Regex r = new Regex(@"^(\w+)\s+(\d+)\s+(\d+):\d+:\d+$");
                Match match = r.Match(strDate);
                if (match.Success)
                {
                    month = monthNumbers[match.Groups[1].ToString()];
                    day = match.Groups[2].ToString();
                    hour = match.Groups[3].ToString();
                }

                lastLogFile = logFile;
            }

            var jsonDate = JsonConvert.SerializeObject(new Dictionary<string, string> { { "Month", month }, { "Day", day }, { "Hour", hour } });

            var strStats = Main.controller.execute("pf", "GetStats", logFile, jsonDate, "COLLECT").output;

            jsonStats = JsonConvert.DeserializeObject<JObject>(strStats);

            var strLogFileList = Main.controller.execute("pf", "GetLogFilesList").output;
            jsonLogFileList = JsonConvert.DeserializeObject<JObject>(strLogFileList);
        }

        void getSelectedLogFile()
        {
            if (logFileOpts2Files.ContainsKey(cbLogFilePicker.Text))
            {
                selectedLogFileOpt = cbLogFilePicker.Text;
                logFile = logFileOpts2Files[selectedLogFileOpt];
            }
        }

        void getSelectedDateTime()
        {
            if (datePicker.SelectedDate != null)
            {
                month = datePicker.SelectedDate.Value.Month.ToString().PadLeft(2, '0');
                day = datePicker.SelectedDate.Value.Day.ToString().PadLeft(2, '0');
                hour = cbHourPicker.Text.PadLeft(2, '0');
            }
        }

        void updateDateTimeText()
        {
            datePicker.Text = day + "." + month;
            cbHourPicker.Text = hour;
        }
    }

    public class StatsHourlyCache : StatsCache
    {
        public string hour;
    }
}
