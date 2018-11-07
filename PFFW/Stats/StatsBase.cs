/*
 * Copyright (C) 2017-2018 Soner Tari
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

using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PFFW
{
    public abstract class StatsBase : UserControl
    {
        public string month = "01";
        public string day = "01";

        public Dictionary<string, ChartCache> chartsCache = new Dictionary<string, ChartCache>();

        protected Dictionary<string, Dictionary<string, ListsCache>> listsCache = new Dictionary<string, Dictionary<string, ListsCache>>();

        protected Dictionary<string, ChartDef> chartDefs = new Dictionary<string, ChartDef>();

        public class ChartDef
        {
            public Color color;
            // For saving the chart state
            public double[] values;
            // For saving the chart state
            public string[] labels;
            public CartesianChart rowChart;
            public CartesianChart columnChart;
            public string total;
            public Label totalLabel;

            public ChartDef(Color c, double[] v, string[] l, CartesianChart rc, CartesianChart cc, string t, Label tl)
            {
                color = c;
                values = v;
                labels = l;
                rowChart = rc;
                columnChart = cc;
                total = t;
                totalLabel = tl;
            }
        }

        protected JObject jsonStats = new JObject();

        protected Dictionary<string, Dictionary<string, ListDef>> listDefs = new Dictionary<string, Dictionary<string, ListDef>>();

        protected class ListDef
        {
            public Dictionary<string, int> data;
            public TextBlock items;
            public TextBlock counts;

            public ListDef(Dictionary<string, int> d, TextBlock i, TextBlock c)
            {
                data = d;
                items = i;
                counts = c;
            }
        };

        protected StatsCache cache;

        public ChartValues<double> totalRowValues { get; set; } = new ChartValues<double>();
        public List<string> totalRowLabels { get; set; } = new List<string>();
        public ChartValues<double> totalColumnValues { get; set; } = new ChartValues<double>();
        public List<string> totalColumnLabels { get; set; } = new List<string>();

        public ChartValues<double> passRowValues { get; set; } = new ChartValues<double>();
        public List<string> passRowLabels { get; set; } = new List<string>();
        public ChartValues<double> passColumnValues { get; set; } = new ChartValues<double>();
        public List<string> passColumnLabels { get; set; } = new List<string>();

        public ChartValues<double> blockRowValues { get; set; } = new ChartValues<double>();
        public List<string> blockRowLabels { get; set; } = new List<string>();
        public ChartValues<double> blockColumnValues { get; set; } = new ChartValues<double>();
        public List<string> blockColumnLabels { get; set; } = new List<string>();

        public ChartValues<double> matchRowValues { get; set; } = new ChartValues<double>();
        public List<string> matchRowLabels { get; set; } = new List<string>();
        public ChartValues<double> matchColumnValues { get; set; } = new ChartValues<double>();
        public List<string> matchColumnLabels { get; set; } = new List<string>();

        protected class ChartValuesLabels
        {
            public ChartValues<double> rowValues;
            public List<string> rowLabels;
            public ChartValues<double> columnValues;
            public List<string> columnLabels;

            public ChartValuesLabels(ChartValues<double> rv, List<string> rl, ChartValues<double> cv, List<string> cl)
            {
                rowValues = rv;
                rowLabels = rl;
                columnValues = cv;
                columnLabels = cl;
            }
        }

        protected Dictionary<string, ChartValuesLabels> chartValuesLabels;

        public Func<ChartPoint, string> PointFormatter { get; set; } = new Func<ChartPoint, string>(point => point.Y.ToString("N0"));

        public StatsBase()
        {
            chartValuesLabels = new Dictionary<string, ChartValuesLabels> {
                { "Total", new ChartValuesLabels(totalRowValues, totalRowLabels, totalColumnValues, totalColumnLabels) },
                { "Pass", new ChartValuesLabels(passRowValues, passRowLabels, passColumnValues, passColumnLabels) },
                { "Block", new ChartValuesLabels(blockRowValues, blockRowLabels, blockColumnValues, blockColumnLabels) },
                { "Match", new ChartValuesLabels(matchRowValues, matchRowLabels, matchColumnValues, matchColumnLabels) },
                };

            init();

            DataContext = this;

            if (!restoreState())
            {
                refresh();
            }
        }

        protected abstract void init();

        protected abstract void refresh();

        virtual public void SaveState()
        {
            cache.month = month;
            cache.day = day;

            foreach (string title in chartDefs.Keys)
            {
                chartsCache[title] = new ChartCache(chartDefs[title].values, chartDefs[title].labels, chartDefs[title].total);

                listsCache[title] = new Dictionary<string, ListsCache>();
                foreach (string key in listDefs[title].Keys)
                {
                    listsCache[title][key] = new ListsCache(listDefs[title][key].data);
                }
            }

            cache.charts = chartsCache;
            cache.lists = listsCache;
        }

        protected abstract bool restoreState();

        protected void restoreStateBase()
        {
            month = cache.month;
            day = cache.day;

            foreach (string title in chartDefs.Keys)
            {
                chartDefs[title].labels = cache.charts[title].labels;
                chartDefs[title].values = cache.charts[title].values;
                updateChart(title);

                chartDefs[title].total = cache.charts[title].total;
                foreach (string key in listDefs[title].Keys)
                {
                    listDefs[title][key].data = cache.lists[title][key].data;
                }
                updateLists(title);
            }
        }

        protected abstract bool isDailyChart();

        protected void fetchStats()
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

        virtual protected void updateStats()
        {
            foreach (string title in chartDefs.Keys)
            {
                updateChartData(title);
                updateChart(title);

                updateListsData(title);
                updateLists(title);
            }
        }

        virtual protected void updateChartData(string title)
        {
            var values = new List<double>();
            var labels = new List<string>();

            if (jsonStats["Date"] != null && jsonStats["Date"].HasValues)
            {
                if (isDailyChart())
                {
                    int i = 0;
                    var it = (jsonStats["Date"] as JObject).GetEnumerator();
                    while (it.MoveNext())
                    {
                        string d = it.Current.Key;
                        string count = "0";
                        if (jsonStats["Date"][d][title] != null)
                        {
                            count = jsonStats["Date"][d][title]["Sum"].ToString();
                        }

                        values.Add(int.Parse(count));
                        labels.Add(d);

                        i++;
                    }
                }
                else
                {
                    var dit = (jsonStats["Date"] as JObject).GetEnumerator();
                    while (dit.MoveNext())
                    {
                        string d = dit.Current.Key;

                        if (jsonStats["Date"][d]["Hours"] != null)
                        {
                            var jsonHourStats = jsonStats["Date"][d]["Hours"];

                            string h = "-1";

                            var it = (jsonHourStats as JObject).GetEnumerator();
                            if (it.MoveNext())
                            {
                                h = it.Current.Key;
                            }

                            for (int i = 0; i < 24; i++)
                            {
                                string count = "0";

                                if (int.Parse(h) == i)
                                {
                                    if (jsonHourStats[h][title] != null && jsonHourStats[h][title]["Sum"] != null)
                                    {
                                        count = jsonHourStats[h][title]["Sum"].ToString();

                                        if (it.MoveNext())
                                        {
                                            h = it.Current.Key;
                                        }
                                    }
                                }

                                if (i < values.Count())
                                {
                                    values[i] += int.Parse(count);
                                }
                                else
                                {
                                    values.Add(int.Parse(count));
                                    // ATTENTION: Make sure we add labels only once
                                    labels.Add(i.ToString().PadLeft(2, '0'));
                                }
                            }
                        }
                    }
                }
            }

            // Update data
            chartDefs[title].values = values.ToArray();
            chartDefs[title].labels = labels.ToArray();
        }


        protected void updateChart(string title)
        {
            // TODO: For performance?
            //chart.Hoverable = false;
            //chart.DataTooltip = null;

            var rowChart = chartDefs[title].rowChart;
            var columnChart = chartDefs[title].columnChart;

            if (isDailyChart())
            {
                if (rowChart != null)
                {
                    if (rowChart.Visibility.Equals(Visibility.Collapsed))
                    {
                        rowChart.Visibility = Visibility.Visible;
                    }

                    if (!rowChart.DisableAnimations)
                    {
                        rowChart.DisableAnimations = true;
                    }

                    chartValuesLabels[title].rowValues.Clear();
                    chartValuesLabels[title].rowValues.AddRange(chartDefs[title].values.ToArray());
                    chartValuesLabels[title].rowLabels.Clear();
                    chartValuesLabels[title].rowLabels.AddRange(chartDefs[title].labels.ToArray());

                    rowChart.Update(true);
                }

                if (columnChart != null && columnChart.Visibility.Equals(Visibility.Visible))
                {
                    columnChart.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (columnChart != null)
                {
                    if (columnChart.Visibility.Equals(Visibility.Collapsed))
                    {
                        columnChart.Visibility = Visibility.Visible;
                    }

                    if (!columnChart.DisableAnimations)
                    {
                        columnChart.DisableAnimations = true;
                    }

                    chartValuesLabels[title].columnValues.Clear();
                    chartValuesLabels[title].columnValues.AddRange(chartDefs[title].values.ToArray());
                    chartValuesLabels[title].columnLabels.Clear();
                    chartValuesLabels[title].columnLabels.AddRange(chartDefs[title].labels.ToArray());

                    columnChart.Update(true);
                }

                if (rowChart != null && rowChart.Visibility.Equals(Visibility.Visible))
                {
                    rowChart.Visibility = Visibility.Collapsed;
                }
            }
        }

        virtual protected void updateListsData(string title)
        {
            // Clear the stats first, in case there is no data
            int count = 0;

            var lists = listDefs[title];
            foreach (string key in lists.Keys)
            {
                lists[key].data.Clear();
            }

            if (jsonStats["Date"] != null && jsonStats["Date"].HasValues)
            {
                var it = (jsonStats["Date"] as JObject).GetEnumerator();
                while (it.MoveNext())
                {
                    var d = it.Current.Key;

                    if (jsonStats["Date"][d][title] != null)
                    {
                        count += int.Parse(jsonStats["Date"][d][title]["Sum"].ToString());

                        foreach (string key in lists.Keys)
                        {
                            var list = lists[key].data;

                            var lit = (jsonStats["Date"][d][title][key] as JObject).GetEnumerator();
                            while (lit.MoveNext())
                            {
                                var sk = lit.Current.Key;
                                var v = jsonStats["Date"][d][title][key][sk].ToString();

                                int c = int.Parse(v);
                                if (list.ContainsKey(sk))
                                {
                                    c += list[sk];
                                }
                                list[sk] = c;
                            }
                        }
                    }
                }
            }

            chartDefs[title].total = count.ToString("N0");
        }

        protected void updateLists(string title)
        {
            chartDefs[title].totalLabel.Content = "total: " + chartDefs[title].total;

            var lists = listDefs[title];

            foreach (string key in lists.Keys)
            {
                var c = "";
                var i = "";
                foreach (var kvp in lists[key].data.OrderByDescending(kvp => kvp.Value))
                {
                    c += kvp.Value + "\n";
                    i += kvp.Key + "\n";
                }

                lists[key].counts.Text = c.TrimEnd();
                lists[key].items.Text = i.TrimEnd();
            }
        }

        protected bool jsonHasKey(object json, string key)
        {
            // TODO: What is the common base class of JObject and JToken?
            var j = json as JObject;
            return j != null && j.HasValues && j[key] != null;
        }
    }

    public class ChartCache
    {
        public double[] values;
        public string[] labels;
        public string total;

        public ChartCache(double[] v, string[] l, string t)
        {
            values = v;
            labels = l;
            total = t;
        }
    }

    public class ListsCache
    {
        public Dictionary<string, int> data;

        public ListsCache(Dictionary<string, int> d)
        {
            data = d;
        }
    }

    public class StatsCache : Cache
    {
        public string month;
        public string day;
        public bool isDailyChart;
        public Dictionary<string, ChartCache> charts;
        public Dictionary<string, Dictionary<string, ListsCache>> lists;
    }
}
