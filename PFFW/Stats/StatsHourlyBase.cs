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
using System.Collections.Generic;

namespace PFFW
{
    public abstract class StatsHourlyBase : StatsBase
    {
        protected JObject jsonHourStats = new JObject();
        protected JObject jsonAllMinuteStats = new JObject();
        protected JObject jsonMinutes = new JObject();

        protected string hour = "00";

        protected void updateJsonVars()
        {
            var date = formatDate();

            // Reset in case there is no data for the requested datetime
            jsonHourStats = new JObject();
            jsonAllMinuteStats = new JObject();

            // TODO: Is there a better way?
            if (jsonHasKey(jsonStats["Date"], date) && jsonHasKey(jsonStats["Date"][date], "Hours") && jsonHasKey(jsonStats["Date"][date]["Hours"], hour))
            {
                jsonHourStats = jsonStats["Date"][date]["Hours"][hour] as JObject;
                if (jsonHasKey(jsonHourStats, "Mins"))
                {
                    jsonAllMinuteStats = jsonHourStats["Mins"] as JObject;
                }
            }
        }

        protected string formatDate()
        {
            return monthNames[month] + " " + day.PadLeft(2, '0');
        }

        override protected bool isDailyChart()
        {
            return false;
        }

        override protected void updateChartData(string title)
        {
            var values = new List<double>();
            var labels = new List<string>();

            int j = 0;

            string m = "-1";

            var it = jsonAllMinuteStats.GetEnumerator();
            if (it.MoveNext())
            {
                m = it.Current.Key;
            }

            for (int i = 0; i < 60; i++)
            {
                string count = "0";

                if (int.Parse(m) == i)
                {
                    var minuteStats = jsonAllMinuteStats[m];

                    if (jsonHasKey(minuteStats, title))
                    {
                        count = minuteStats[title].ToString();
                    }

                    j++;
                    if (it.MoveNext())
                    {
                        m = it.Current.Key;
                    }
                }

                values.Add(int.Parse(count));
                labels.Add(i.ToString().PadLeft(2, '0'));
            }

            // Update data
            chartDefs[title].values = values.ToArray();
            chartDefs[title].labels = labels.ToArray();
        }

        override protected void updateListsData(string title)
        {
            // Clear the stats first, in case there is no data
            var lists = listDefs[title];
            foreach (string key in lists.Keys)
            {
                lists[key].data.Clear();
            }

            if (jsonHasKey(jsonHourStats, title))
            {
                var cks = jsonHourStats[title];

                chartDefs[title].total = cks["Sum"].ToString();

                var chartKeys = listDefs[title].Keys;

                foreach (string key in chartKeys)
                {
                    var list = listDefs[title][key].data;

                    var vs = cks[key];

                    var it = (vs as JObject).GetEnumerator();
                    while (it.MoveNext())
                    {
                        string sk = it.Current.Key;
                        string v = vs[sk].ToString();
                        list[sk] = int.Parse(v);
                    }
                }
            }
        }
    }
}
