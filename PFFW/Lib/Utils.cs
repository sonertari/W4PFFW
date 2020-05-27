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

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace PFFW
{
    static class Utils
    {
        // ATTENTION: Do not translate month names, they are used to match the strings in log files in English
        public static readonly Dictionary<string, string> monthNames = new Dictionary<string, string> {
            { "01", "Jan" },
            { "02", "Feb" },
            { "03", "Mar" },
            { "04", "Apr" },
            { "05", "May" },
            { "06", "Jun" },
            { "07", "Jul" },
            { "08", "Aug" },
            { "09", "Sep" },
            { "10", "Oct" },
            { "11", "Nov" },
            { "12", "Dec" }
            };

        public static readonly Dictionary<string, string> monthNumbers;

        static Utils()
        {
            monthNumbers = monthNames.ToDictionary(pair => pair.Value, pair => pair.Key);
        }

        public static string[][] jsonToStringArray(JArray jsonArr, List<string> keys, bool addNumber = true, int offset = 0)
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
}
