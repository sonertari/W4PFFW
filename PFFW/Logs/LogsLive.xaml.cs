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
using System.Timers;
using System.Windows;

namespace PFFW
{
    public partial class LogsLive : LogsBase
    {
        private string logFile = "";

        Timer timer;
        int refreshTimeout = 10;
        bool timerEventRunning = false;

        override protected void init()
        {
            InitializeComponent();

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

            cache = new LogsBaseCache();

            base.SaveState();
            cache.logFile = logFile;

            Main.self.cache["LogsLive"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("LogsLive"))
            {
                cache = Main.self.cache["LogsLive"] as LogsBaseCache;

                base.restoreState();
                logFile = cache.logFile;

                updateSelections();
                updateLogsView();

                return true;
            }
            return false;
        }

        override protected void fetch()
        {
            getSelections();

            logFile = Main.controller.execute("pf", "GetDefaultLogFile").output;

            mLogSize = int.Parse(Main.controller.execute("pf", "GetFileLineCount", logFile, mRegex).output);

            mLogs = Main.controller.execute("pf", "GetLiveLogs", logFile, mLinesPerPage, mRegex).output;

            var strReloadRate = Main.controller.execute("pf", "GetReloadRate").output;

            int timeout = int.Parse(strReloadRate);
            refreshTimeout = timeout < 10 ? 10 : timeout;
        }

        private void getSelections()
        {
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

        private void button_Click(object sender, RoutedEventArgs e)
        {
            refresh();
        }

        override protected void updateView()
        {
            updateSelections();
            updateLogsView();
        }

        protected void updateLogsView()
        {
            int lineCount = 0;
            if (mLogSize > mLinesPerPage)
            {
                lineCount = mLogSize - mLinesPerPage;
            }

            var jsonArr = JsonConvert.DeserializeObject<JArray>(mLogs);
            logsDataGrid.ItemsSource = Utils.jsonToStringArray(jsonArr, new List<string> { "Rule", "Date", "Time", "Act", "Dir", "If", "SrcIP", "SPort", "DstIP", "DPort", "Type", "Log" }, true, lineCount);
        }

        private void updateSelections()
        {
            linesPerPage.Text = mLinesPerPage.ToString();
            regex.Text = mRegex;
        }
    }
}
