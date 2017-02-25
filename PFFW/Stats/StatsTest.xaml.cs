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
using System.Windows.Media;
using System.Timers;
using System.Windows.Controls;
using LiveCharts.Wpf;
using LiveCharts;

namespace PFFW
{
    /// <summary>
    /// Test memory leak due to chart recreation.
    /// </summary>
    public partial class StatsTest : UserControl
    {
        Timer timer;
        int refreshTimeout = 5;
        bool timerEventRunning = false;

        public StatsTest()
        {
            InitializeComponent();

            refresh();

            timer = new Timer(refreshTimeout * 1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        public void SaveState()
        {
            timer.Stop();
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
                finally
                {
                    timerEventRunning = false;

                }
            }
        }

        protected void refresh()
        {
            cleanUp();
            content.Content = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true);

            var chart = new CartesianChart();

            chart.Series = new SeriesCollection();

            Series series;
            series = new ColumnSeries { LabelPoint = point => point.Y.ToString("N0") };

            series.Title = "title";
            series.Fill = new SolidColorBrush(Colors.Red);
            series.DataLabels = true;

            series.Values = new ChartValues<double>(new List<double> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            chart.Series.Add(series);

            chart.AxisX = new AxesCollection();
            chart.AxisY = new AxesCollection();

            chart.AxisX.Add(new Axis { Title = "Hours", Labels = new List<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" } });
            chart.AxisY.Add(new Axis { Title = "Stats" });

            content.Content = chart;
        }

        // Desparate clean up attempt to free the chart, which helps a bit btw.
        void cleanUp()
        {
            if (content.HasContent)
            {
                var prevChart = content.Content as CartesianChart;

                prevChart.DisableAnimations = true;
                prevChart.Hoverable = false;
                prevChart.DataTooltip = null;

                (prevChart.Series[0] as Series).Title = "";
                (prevChart.Series[0] as Series).Fill = new SolidColorBrush();
                prevChart.Series[0].Values = new ChartValues<double>();
                prevChart.Series.Clear();
                prevChart.Series = null;

                prevChart.AxisX[0].Labels = new List<string>();
                prevChart.AxisX.Clear();
                prevChart.AxisX = null;

                prevChart.AxisY[0].Labels = new List<string>();
                prevChart.AxisY.Clear();
                prevChart.AxisY = null;

                prevChart.Content = null;
                prevChart = null;
            }
        }
    }
}
