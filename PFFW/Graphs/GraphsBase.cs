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
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PFFW
{
    public abstract class GraphsBase : UserControl
    {
        protected string name;

        protected string layout;

        protected GraphsCache cache;

        protected Dictionary<string, Image> images;
        protected Dictionary<string, BitmapImage> bitmaps;

        Timer timer;
        protected int refreshTimeout = 10;
        bool timerEventRunning = false;

        public GraphsBase()
        {
            name = GetType().Name;

            init();

            if (!restoreState())
            {
                refresh();
            }

            timer = new Timer(refreshTimeout * 1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

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
            timer.Stop();

            cache = new GraphsCache();

            cache.bitmaps = bitmaps;

            Main.self.cache[name] = cache;
        }

        protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey(name))
            {
                cache = Main.self.cache[name] as GraphsCache;

                bitmaps = cache.bitmaps;
                updateView();

                return true;
            }
            return false;
        }

        protected void refresh()
        {
            fetchGraphs();
            updateView();
        }

        protected void fetchGraphs()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                // TODO: Find the correct way of getting the actual height of the client area
                var strGraphs = Main.controller.execute("symon", "RenderLayout", layout, Math.Round(Main.windowWidth), Math.Round(Main.windowHeight - 220)).output;

                createBitmaps(strGraphs);

                var strReloadRate = Main.controller.execute("pf", "GetReloadRate").output;

                int timeout = int.Parse(strReloadRate);
                refreshTimeout = timeout < 10 ? 10 : timeout;
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

        protected void createBitmaps(string strGraphs)
        {
            var graphs = JsonConvert.DeserializeObject<Dictionary<string, string>>(strGraphs);

            foreach (var key in graphs.Keys)
            {
                var file = graphs[key];

                System.IO.MemoryStream stream = null;
                try
                {
                    // TODO: Check why output has escaped double quotes around it
                    var base64Graph = Main.controller.execute("symon", "GetGraph", file).output.Trim('\\').Trim('"');
                    stream = new System.IO.MemoryStream(Convert.FromBase64String(base64Graph));
                }
                catch (Exception e)
                {
                    MessageBox.Show("Exception: " + e.Message);
                }

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                if (stream != null)
                {
                    bmp.StreamSource = stream;
                }
                bmp.EndInit();

                bitmaps[key] = bmp;
            }
        }

        protected void updateView()
        {
            foreach (var key in images.Keys)
            {
                images[key].Source = bitmaps[key];
            }
        }
    }

    public class GraphsCache : Cache
    {
        public Dictionary<string, BitmapImage> bitmaps;
    }
}
