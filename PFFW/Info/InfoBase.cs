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
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PFFW
{
    public abstract class InfoBase : UserControl
    {
        protected Cache cache;

        Timer timer;
        protected int refreshTimeout = 10;
        bool timerEventRunning = false;

        public InfoBase()
        {
            init();

            if (!restoreState())
            {
                refresh();
            }

            timer = new Timer(refreshTimeout * 1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        /// <summary>
        /// Page specific initialization.
        /// </summary>
        protected abstract void init();

        delegate void RefreshDelegate();

        protected void Timer_Elapsed(object sender, ElapsedEventArgs e)
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

        virtual public void SaveState()
        {
            // ATTENTION: Do not forget to stop the timer.
            timer.Stop();
        }

        protected abstract bool restoreState();

        protected void refresh()
        {
            fetchInfo();
            updateView();
        }

        protected void fetchInfo()
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

        /// <summary>
        /// Fetch the data from the PFFW server.
        /// </summary>
        protected abstract void fetch();

        /// <summary>
        /// Push the data to the view.
        /// </summary>
        protected abstract void updateView();

        protected string[][] jsonToStringArray(JArray jsonArr, List<string> keys, bool addNumber = true)
        {
            var a = new List<string[]>();
            foreach (var d in jsonArr.ToObject<List<Dictionary<string, string>>>())
            {
                var l = new List<string>();

                if (addNumber)
                {
                    l.Add((a.Count + 1).ToString());
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
}
