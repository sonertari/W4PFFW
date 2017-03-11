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

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PFFW
{
    public partial class Main : Window
    {
        private static Main _self;
        public static Main self
        {
            get
            {
                return _self;
            }
            set
            {
                if (_self == null)
                {
                    _self = value;
                }
            }
        }

        public readonly static Controller controller = new Controller();

        /// <summary>
        /// Pages save their state vars in this dictionary, each page with its class name as unique key.
        /// </summary>
        public Dictionary<string, Cache> cache = new Dictionary<string, Cache>();

        /// <summary>
        /// Jump table from menu items to pages.
        /// Used while instantiating pages.
        /// </summary>
        Dictionary<MenuItem, Type> pages;

        /// <summary>
        /// Holds the currently displayed page.
        /// Init to a generic UserControl, so that showPage() does not give null exception for it during application start
        /// </summary>
        UserControl page = new UserControl();

        /// <summary>
        /// We use these dimensions while generating graphs
        /// </summary>
        public static double windowWidth;
        public static double windowHeight;

        public Main()
        {
            InitializeComponent();

            self = this;

            pages = new Dictionary<MenuItem, Type>() {
                {InfoPfMenuItem, typeof(InfoPf)},
                //{InfoPfMVVMMenuItem, typeof(InfoPfMVVM)},
                {InfoSystemMenuItem, typeof(InfoSystem)},
                {InfoHostsMenuItem, typeof(InfoHosts)},
                {InfoIfsMenuItem, typeof(InfoIfs)},
                {InfoRulesMenuItem, typeof(InfoRules)},
                {InfoStatesMenuItem, typeof(InfoStates)},
                {InfoQueuesMenuItem, typeof(InfoQueues)},
                {StatsGeneralMenuItem, typeof(StatsGeneral)},
                {StatsDailyMenuItem, typeof(StatsDaily)},
                {StatsHourlyMenuItem, typeof(StatsHourly)},
                {StatsLiveMenuItem, typeof(StatsLive)},
                {GraphsIfsMenuItem, typeof(GraphsIfs)},
                {GraphsTransferMenuItem, typeof(GraphsTransfer)},
                {GraphsStatesMenuItem, typeof(GraphsStates)},
                {GraphsMbufsMenuItem, typeof(GraphsMbufs)},
                {LogsArchivesMenuItem, typeof(LogsArchives)},
                {LogsLiveMenuItem, typeof(LogsLive)},
            };

            SizeChanged += OnWindowSizeChanged;

            logOut();
        }

        /// <summary>
        /// Used while generating graphs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // ATTENTION: Do not use e.NewSize, we need the size of the client area
            //windowWidth = e.NewSize.Width;
            //windowHeight = e.NewSize.Height - menu.ActualHeight;
            windowWidth = ((Panel)Application.Current.MainWindow.Content).ActualWidth;
            windowHeight = ((Panel)Application.Current.MainWindow.Content).ActualHeight - menu.ActualHeight;
        }

        public void loggedIn()
        {
            if (!controller.host.Equals(controller.previousHost))
            {
                // Reset the cache if the host changes
                cache = new Dictionary<string, Cache>();
                controller.previousHost = controller.host;
            }
            menu.Visibility = Visibility.Visible;
            showPage(typeof(InfoPf));
        }

        public void logOut()
        {
            controller.logOut();

            menu.Visibility = Visibility.Hidden;
            showPage(typeof(Login));
        }

        public void exit()
        {
            controller.logOut();
            Application.Current.Shutdown();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(LogOut))
            {
                logOut();
            }
            else if (sender.Equals(Exit))
            {
                exit();
            }
            else
            {
                showPage(pages[sender as MenuItem]);
            }
        }

        /// <summary>
        /// Show the page.
        /// </summary>
        /// <param name="p">Page type which extends UserControl</param>
        public void showPage(Type p)
        {
            // Allow page to save its state vars, if any.
            // These vars are used to populate controls without fetching any data remotely,
            // if/when the user comes back to the same page.
            var saveState = page.GetType().GetMethod("SaveState");
            if (saveState != null)
            {
                saveState.Invoke(page, null);
            }

            page = p.GetConstructor(Type.EmptyTypes).Invoke(null) as UserControl;
            content.Content = page;
        }
    }

    public class Cache
    {
        // XXX: Not all pages need these log vars.
        public string logFile = "";
        public string lastLogFile = "";
        public string selectedLogFileOpt = "";
        public List<string> logFileOpts = new List<string>();
        public Dictionary<string, string> logFileOpts2Files = new Dictionary<string, string>();
    }
}