/*
 * Copyright (C) 2017-2020 Soner Tari
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

using System.Windows.Media.Imaging;

namespace PFFW
{
    /// <summary>
    /// Pf information.
    /// </summary>
    public partial class InfoPf : InfoBase
    {
        private int mPfStatus;
        private string mPfInfo;
        private string mPfMem;
        private string mPfTimeout;

        override protected void init()
        {
            InitializeComponent();
        }

        override public void SaveState()
        {
            base.SaveState();

            cache = new InfoPfCache();

            (cache as InfoPfCache).mPfStatus = mPfStatus;
            (cache as InfoPfCache).mPfInfo = mPfInfo;
            (cache as InfoPfCache).mPfMem = mPfMem;
            (cache as InfoPfCache).mPfTimeout = mPfTimeout;

            Main.self.cache["InfoPf"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("InfoPf"))
            {
                cache = Main.self.cache["InfoPf"] as InfoPfCache;

                mPfStatus = (cache as InfoPfCache).mPfStatus;
                mPfInfo = (cache as InfoPfCache).mPfInfo;
                mPfMem = (cache as InfoPfCache).mPfMem;
                mPfTimeout = (cache as InfoPfCache).mPfTimeout;

                updateView();

                return true;
            }
            return false;
        }

        override protected void fetch()
        {
            mPfStatus = Main.controller.execute("pf", "IsRunning").status;
            mPfInfo = Main.controller.execute("pf", "GetPfInfo").output;
            mPfMem = Main.controller.execute("pf", "GetPfMemInfo").output;
            mPfTimeout = Main.controller.execute("pf", "GetPfTimeoutInfo").output;

            var strReloadRate = Main.controller.execute("pf", "GetReloadRate").output;

            int timeout = int.Parse(strReloadRate);
            refreshTimeout = timeout < 10 ? 10 : timeout;
        }

        override protected void updateView()
        {
            pfStatusImage.Source = Resources[mPfStatus == 0 ? "run" : "stop"] as BitmapImage;
            pfStatus.Content = mPfStatus == 0 ? "PF is running" : "PF is not running";
            pfInfo.Text = mPfInfo;
            pfMemInfo.Text = mPfMem;
            pfTimeoutInfo.Text = mPfTimeout;
        }
    }

    public class InfoPfCache : Cache
    {
        public int mPfStatus;
        public string mPfInfo;
        public string mPfMem;
        public string mPfTimeout;
    }
}
