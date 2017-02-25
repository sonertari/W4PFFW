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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PFFW
{
    public abstract class LogsBase : UserControl
    {
        protected Cache cache;

        protected string mLogs;

        protected int mLinesPerPage, mLogSize = 0;
        protected string mRegex = "";

        protected string logFile = "";

        public LogsBase()
        {
            init();

            if (!restoreState())
            {
                refresh();
            }
        }

        protected abstract void init();

        virtual public void SaveState()
        {
            cache.logFile = logFile;

            (cache as LogsBaseCache).mLogs = mLogs;
            (cache as LogsBaseCache).mLinesPerPage = mLinesPerPage;
            (cache as LogsBaseCache).mLogSize = mLogSize;
            (cache as LogsBaseCache).mRegex = mRegex;
        }

        virtual protected bool restoreState()
        {
            logFile = cache.logFile;

            mLogs = (cache as LogsBaseCache).mLogs;
            mLinesPerPage = (cache as LogsBaseCache).mLinesPerPage;
            mLogSize = (cache as LogsBaseCache).mLogSize;
            mRegex = (cache as LogsBaseCache).mRegex;

            return true;
        }

        protected void refresh()
        {
            fetchLogs();
            updateView();
        }

        protected void fetchLogs()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                fetch();
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception: " + e.Message);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        protected abstract void fetch();

        protected abstract void updateView();

        protected void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var cells = e.Row.Item as string[];
            if (cells != null)
            {
                var act = cells[4];
                if (act.Equals("pass"))
                {
                    e.Row.Background = new SolidColorBrush(Colors.Lime);
                }
                else if (act.Equals("block"))
                {
                    e.Row.Background = new SolidColorBrush(Colors.Red);
                    e.Row.Foreground = new SolidColorBrush(Colors.White);
                }
                else if (act.Equals("match"))
                {
                    e.Row.Background = new SolidColorBrush(Colors.Yellow);
                }
            }
        }

        // XXX: Code reuse?
        protected string[][] jsonToStringArray(JArray jsonArr, List<string> keys, bool addNumber = true, int offset = 0)
        {
            var a = new List<string[]>();
            foreach (var d in jsonArr.ToObject<List<Dictionary<string, string>>>())
            {
                var l = new List<string>();

                if (addNumber)
                {
                    l.Add((a.Count + offset + 1).ToString());
                }

                foreach (var k in keys)
                {
                    if (d.ContainsKey(k))
                    {
                        l.Add(d[k]);
                    }
                }
                a.Add(l.ToArray());
            }
            return a.ToArray();
        }
    }

    public class LogsBaseCache : Cache
    {
        public string mLogs;
        public int mLinesPerPage, mLogSize;
        public string mRegex;
    }
}
