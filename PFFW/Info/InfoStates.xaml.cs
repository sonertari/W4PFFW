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
    /// <summary>
    /// Pf states.
    /// </summary>
    public partial class InfoStates : InfoBase
    {
        private string mStates;

        private int mLinesPerPage, mStateSize = 0, mStartLine, mHeadStart;
        private string mRegex = "";

        private object mButton;
        private bool mButtonPressed = false;

        override protected void init()
        {
            InitializeComponent();
        }

        override public void SaveState()
        {
            base.SaveState();

            cache = new InfoStatesCache();

            (cache as InfoStatesCache).mStates = mStates;
            (cache as InfoStatesCache).mLinesPerPage = mLinesPerPage;
            (cache as InfoStatesCache).mStateSize = mStateSize;
            (cache as InfoStatesCache).mStartLine = mStartLine;
            (cache as InfoStatesCache).mHeadStart = mHeadStart;
            (cache as InfoStatesCache).mRegex = mRegex;

            Main.self.cache["InfoStates"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("InfoStates"))
            {
                cache = Main.self.cache["InfoStates"] as InfoStatesCache;

                mStates = (cache as InfoStatesCache).mStates;
                mLinesPerPage = (cache as InfoStatesCache).mLinesPerPage;
                mStateSize = (cache as InfoStatesCache).mStateSize;
                mStartLine = (cache as InfoStatesCache).mStartLine;
                mHeadStart = (cache as InfoStatesCache).mHeadStart;
                mRegex = (cache as InfoStatesCache).mRegex;

                updateView();

                return true;
            }
            return false;
        }

        override protected void fetch()
        {
            getSelections();

            mStateSize = int.Parse(Main.controller.execute("pf", "GetStateCount", mRegex).output);

            computeNavigationVars();

            mStates = Main.controller.execute("pf", "GetStateList", mHeadStart, mLinesPerPage, mRegex).output;

            var strReloadRate = Main.controller.execute("pf", "GetReloadRate").output;

            int timeout = int.Parse(strReloadRate);
            refreshTimeout = timeout < 10 ? 10 : timeout;
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

        // TODO: This method is the exact replica of the one used by LogsArchives page. Find a way to to combine them.
        // There are too many vars to pass though.
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
                    mStartLine = mStateSize;
                }
                mButtonPressed = false;
            }

            mHeadStart = mStartLine + mLinesPerPage;
            if (mHeadStart > mStateSize)
            {
                mHeadStart = mStateSize;
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

            var jsonArr = JsonConvert.DeserializeObject<JArray>(mStates);
            var arr = JsonConvert.DeserializeObject<List<List<string>>>(jsonArr.ToString());

            // Prepend line numbers
            int i = mStartLine;
            foreach (List<string> row in arr)
            {
                row.Insert(0, (++i).ToString());
            }

            statesDataGrid.ItemsSource = arr.ToArray();
        }

        private void updateSelections()
        {
            stateSize.Content = "/" + mStateSize;

            startLine.Text = (mStartLine + 1).ToString();
            linesPerPage.Text = mLinesPerPage.ToString();
            regex.Text = mRegex;
        }
    }

    public class InfoStatesCache : Cache
    {
        public string mStates;
        public int mLinesPerPage, mStateSize, mStartLine, mHeadStart;
        public string mRegex;
    }
}
