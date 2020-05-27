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

using System;
using System.Windows;
using System.Windows.Controls;

namespace PFFW
{
    public partial class Login : UserControl
    {
        public Login()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int po = 22;
                try
                {
                    po = int.Parse(port.Text);
                }
                catch
                {
                    port.Text = "22";
                }

                if (Main.controller.logIn(user.Text, EasyEncryption.SHA.ComputeSHA1Hash(password.Password), host.Text, po))
                {
                    Main.self.loggedIn();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception: " + ex.Message);
            }
        }
    }
}
