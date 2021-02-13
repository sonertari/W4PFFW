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
using System;
using System.Collections.Generic;
using System.Windows;

namespace PFFW
{
    public partial class LogsArchives : LogsBase
    {
        private int mStartLine, mHeadStart;

        private object mButton;
        private bool mButtonPressed = false;

        override protected void init()
        {
            InitializeComponent();
        }

        override public void SaveState()
        {
            cache = new LogsArchivesCache();

            base.SaveState();
            logFilePicker.SaveState(cache);

            (cache as LogsArchivesCache).mStartLine = mStartLine;
            (cache as LogsArchivesCache).mHeadStart = mHeadStart;

            Main.self.cache["LogsArchives"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("LogsArchives"))
            {
                cache = Main.self.cache["LogsArchives"] as LogsArchivesCache;

                base.restoreState();
                logFilePicker.restoreState(cache);

                mStartLine = (cache as LogsArchivesCache).mStartLine;
                mHeadStart = (cache as LogsArchivesCache).mHeadStart;

                updateSelections();
                updateLogsView();

                return true;
            }
            return false;
        }

        override protected void fetch()
        {
            getSelections();

            var logfile = logFilePicker.selectLogFile();

            mLogSize = int.Parse(Main.controller.execute("pf", "GetFileLineCount", logfile, mRegex).output);

            computeNavigationVars();

            mLogs = Main.controller.execute("pf", "GetLogs", logfile, mHeadStart, mLinesPerPage, mRegex).output;

            logFilePicker.fetch();
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
                // Fixed on OpenBSD 6.4
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
            logFilePicker.refresh();
            updateLogsView();
        }

        protected void updateLogsView()
        {
            var jsonArr = JsonConvert.DeserializeObject<JArray>(mLogs);
            logsDataGrid.ItemsSource = Utils.jsonToStringArray(jsonArr, new List<string> { "Rule", "Date", "Time", "Act", "Dir", "If", "SrcIP", "SPort", "DstIP", "DPort", "Type", "Log" }, true, mStartLine);
        }

        private void updateSelections()
        {
            stateSize.Content = "/" + mLogSize;

            startLine.Text = (mStartLine + 1).ToString();
            linesPerPage.Text = mLinesPerPage.ToString();
            regex.Text = mRegex;

            selected.Content = "Selected: " + logFilePicker.getSelectedLogFileOpt();
        }
    }

    public class LogsArchivesCache : LogsBaseCache
    {
        public int mStartLine, mHeadStart;
    }
}
