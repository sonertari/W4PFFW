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

using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PFFW
{
    public partial class GraphsTransfer : GraphsBase
    {
        override protected void init()
        {
            InitializeComponent();

            layout = "transfer";
            
            bitmaps = new Dictionary<string, BitmapImage> {
                { "Data Transfer", null }
                };

            images = new Dictionary<string, Image> {
                { "Data Transfer", transferImage },
                };
        }
    }
}
