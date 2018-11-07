/*
 * Copyright (C) 2017-2018 Soner Tari
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace PFFW
{
    class LogFilePicker : ComboBox
    {
        private string _logFile = "";
        public string logFile
        {
            get
            {
                if (logFileOpts2Files.ContainsKey(Text))
                {
                    selectedLogFileOpt = Text;
                    _logFile = logFileOpts2Files[selectedLogFileOpt];
                }
                return _logFile;
            }
            set
            {
                _logFile = value;
            }
        }

        private string lastLogFile = "";

        private string selectedLogFileOpt = "";

        private JObject jsonLogFileList = new JObject();

        private Dictionary<string, string> logFileOpts2Files = new Dictionary<string, string>();
        private List<string> logFileOpts = new List<string>();

        public string getSelectedLogFileOpt()
        {
            return selectedLogFileOpt;
        }

        private bool isLogFileChanged()
        {
            return !logFile.Equals(lastLogFile);
        }

        public string selectLogFile()
        {
            var l = Main.controller.execute("pf", "SelectLogFile", logFile).output;
            // This is neccessary during init, otherwise the displayed Text remains empty
            logFile = l;
            return l;
        }

        public Tuple<string, string, string> getLogStartDate(string month, string day)
        {
            string hour = "";

            if (isLogFileChanged())
            {
                var strDate = Main.controller.execute("pf", "GetLogStartDate", logFile).output;

                Regex r = new Regex(@"^(\w+)\s+(\d+)\s+(\d+):\d+:\d+$");
                Match match = r.Match(strDate);
                if (match.Success)
                {
                    month = Utils.monthNumbers[match.Groups[1].ToString()];
                    day = match.Groups[2].ToString();
                    hour = match.Groups[3].ToString();
                }

                lastLogFile = logFile;
            }
            return new Tuple<string, string, string>(month, day, hour);
        }

        public void fetch()
        {
            var strLogFileList = Main.controller.execute("pf", "GetLogFilesList").output;
            jsonLogFileList = JsonConvert.DeserializeObject<JObject>(strLogFileList);
        }

        public void refresh()
        {
            // ATTENTION: Log file may change after the log file list is updated
            updateLogFileLists();
            updateLogFilePicker();
        }

        private void updateLogFileLists()
        {
            logFileOpts.Clear();
            logFileOpts2Files.Clear();

            // Clone to create a local copy, because we modify this local copy below
            var logfile = logFile;

            var it = jsonLogFileList.GetEnumerator();
            while (it.MoveNext())
            {
                var file = it.Current.Key;

                var optFileBasename = Path.GetFileNameWithoutExtension(file);

                var opt = jsonLogFileList[file] + " - " + optFileBasename;

                logFileOpts.Add(opt);
                logFileOpts2Files[opt] = file;

                // XXX: Need the inverse of mLogFileOpts2Files list to get selectedLogFileOpt easily
                // ATTENTION: But the keys of the inverse list are not suitable, because mLogFile may refer to a tmp file: /var/tmp/pffw/logs/Pf/pflog
                if (Path.GetExtension(file).Equals(".gz"))
                {
                    // logFile does not have .gz extension, because it points to the file decompressed by the controller
                    // Update this local copy for comparison and to print it below
                    var logFileBasename = Path.GetFileName(file);
                    logfile += ((logFileBasename + ".gz").Equals(optFileBasename)) ? ".gz" : "";
                }

                if (optFileBasename.Equals(Path.GetFileName(logfile)))
                {
                    selectedLogFileOpt = opt;
                }
            }
        }

        public void updateLogFilePicker()
        {
            // TODO: Make this work?
            //cbLogFilePicker.ItemsSource = logFileOpts;
            Items.Clear();
            logFileOpts.ForEach(item => Items.Add(item));
            Text = selectedLogFileOpt;
        }

        public void SaveState(Cache cache)
        {
            cache.logFile = logFile;
            cache.lastLogFile = lastLogFile;
            cache.selectedLogFileOpt = selectedLogFileOpt;
            cache.logFileOpts = logFileOpts;
            cache.logFileOpts2Files = logFileOpts2Files;
        }

        public void restoreState(Cache cache)
        {
            logFile = cache.logFile;
            lastLogFile = cache.lastLogFile;
            selectedLogFileOpt = cache.selectedLogFileOpt;
            logFileOpts = cache.logFileOpts;
            logFileOpts2Files = cache.logFileOpts2Files;

            updateLogFilePicker();
        }
    }
}
