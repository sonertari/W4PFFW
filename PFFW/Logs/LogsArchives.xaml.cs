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
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace PFFW
{
    public partial class LogsArchives : LogsBase
    {
        private int mStartLine, mHeadStart;

        private object mButton;
        private bool mButtonPressed = false;

        public string selectedLogFileOpt = "";

        protected Dictionary<string, string> logFileOpts2Files = new Dictionary<string, string>();
        protected List<string> logFileOpts = new List<string>();

        protected JObject jsonLogFileList = new JObject();

        override protected void init()
        {
            InitializeComponent();
        }

        override public void SaveState()
        {
            cache = new LogsArchivesCache();

            base.SaveState();

            (cache as LogsArchivesCache).mStartLine = mStartLine;
            (cache as LogsArchivesCache).mHeadStart = mHeadStart;

            cache.selectedLogFileOpt = selectedLogFileOpt;
            cache.logFileOpts = logFileOpts;
            cache.logFileOpts2Files = logFileOpts2Files;

            Main.self.cache["LogsArchives"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("LogsArchives"))
            {
                cache = Main.self.cache["LogsArchives"] as LogsArchivesCache;

                base.restoreState();

                mStartLine = (cache as LogsArchivesCache).mStartLine;
                mHeadStart = (cache as LogsArchivesCache).mHeadStart;

                selectedLogFileOpt = cache.selectedLogFileOpt;
                logFileOpts = cache.logFileOpts;
                logFileOpts2Files = cache.logFileOpts2Files;

                updateSelections();
                updateLogFilePicker();
                updateLogsView();

                return true;
            }
            return false;
        }

        override protected void fetch()
        {
            getSelectedLogFile();
            getSelections();

            logFile = Main.controller.execute("pf", "SelectLogFile", logFile).output;

            mLogSize = int.Parse(Main.controller.execute("pf", "GetFileLineCount", logFile, mRegex).output);

            computeNavigationVars();

            mLogs = Main.controller.execute("pf", "GetLogs", logFile, mHeadStart, mLinesPerPage, mRegex).output;

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

        private void getSelections()
        {
            try
            {
                mStartLine = int.Parse(startLine.Text) - 1;
            }
            catch
            {
                mStartLine = 0;
            }

            try
            {
                // ATTENTION: Never allow too large numbers here.
                // BUG: tail(1) on OpenBSD 5.9 amd64 gets stuck with: echo soner | /usr/bin/tail -99999999
                mLinesPerPage = Math.Min(999, int.Parse(linesPerPage.Text));
            }
            catch
            {
                mLinesPerPage = 25;
            }
            mRegex = regex.Text;
        }

        // XXX: Code reuse.
        private void computeNavigationVars()
        {
            if (mButtonPressed)
            {
                if (mButton.Equals(btnFirst))
                {
                    mStartLine = 0;
                }
                else if (mButton.Equals(btnPrevious))
                {
                    mStartLine -= mLinesPerPage;
                }
                else if (mButton.Equals(btnNext))
                {
                    mStartLine += mLinesPerPage;
                }
                else if (mButton.Equals(btnLast))
                {
                    mStartLine = mLogSize;
                }
                mButtonPressed = false;
            }

            mHeadStart = mStartLine + mLinesPerPage;
            if (mHeadStart > mLogSize)
            {
                mHeadStart = mLogSize;
                mStartLine = mHeadStart - mLinesPerPage;
            }
            if (mStartLine < 0)
            {
                mStartLine = 0;
                mHeadStart = mLinesPerPage;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            mButtonPressed = true;
            mButton = sender;

            refresh();
        }

        override protected void updateView()
        {
            updateSelections();
            updateLogFileLists();
            updateLogFilePicker();
            updateLogsView();
        }

        protected void updateLogsView()
        {
            var jsonArr = JsonConvert.DeserializeObject<JArray>(mLogs);
            logsDataGrid.ItemsSource = jsonToStringArray(jsonArr, new List<string> { "Rule", "Date", "Time", "Act", "Dir", "If", "SrcIP", "SPort", "DstIP", "DPort", "Type", "Log" }, true, mStartLine);
        }

        private void updateSelections()
        {
            stateSize.Content = "/" + mLogSize;

            startLine.Text = (mStartLine + 1).ToString();
            linesPerPage.Text = mLinesPerPage.ToString();
            regex.Text = mRegex;

            selected.Content = "Selected: " + selectedLogFileOpt;
        }

        // XXX: Code reuse.
        public void updateLogFileLists()
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

        // XXX: Code reuse.
        private void updateLogFilePicker()
        {
            // TODO: Make this work?
            //cbLogFilePicker.ItemsSource = logFileOpts;
            cbLogFilePicker.Items.Clear();
            logFileOpts.ForEach(item => cbLogFilePicker.Items.Add(item));
            cbLogFilePicker.Text = selectedLogFileOpt;
        }
    }

    public class LogsArchivesCache : LogsBaseCache
    {
        public int mStartLine, mHeadStart;
    }
}
