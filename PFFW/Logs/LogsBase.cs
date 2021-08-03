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
            (cache as LogsBaseCache).mLogs = mLogs;
            (cache as LogsBaseCache).mLinesPerPage = mLinesPerPage;
            (cache as LogsBaseCache).mLogSize = mLogSize;
            (cache as LogsBaseCache).mRegex = mRegex;
        }

        virtual protected bool restoreState()
        {
            mLogs = (cache as LogsBaseCache).mLogs;
            mLinesPerPage = (cache as LogsBaseCache).mLinesPerPage;
            mLogSize = (cache as LogsBaseCache).mLogSize;
            mRegex = (cache as LogsBaseCache).mRegex;

            return true;
        }

        protected void refresh()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                fetch();
                updateView();
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
    }

    public class LogsBaseCache : Cache
    {
        public string mLogs;
        public int mLinesPerPage, mLogSize;
        public string mRegex;
    }
}
