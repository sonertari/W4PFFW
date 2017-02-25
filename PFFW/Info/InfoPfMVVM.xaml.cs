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
using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PFFW
{
    /// <summary>
    /// Example MVVM version of the Pf information page.
    /// 
    /// XXX: This is really overkill for such a simple UserControl:
    /// https://blogs.msdn.microsoft.com/johngossman/2006/03/04/advantages-and-disadvantages-of-m-v-vm/
    /// This could be achieved simply by 2x calls to a UI update method.
    /// Besides, perhaps I don't want to update the UI until after all variables are updated.
    /// So, see the implementation of the InfoPf class.
    /// </summary>
    public partial class InfoPfMVVM : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        private int _mPfStatus;
        public int mPfStatus
        {
            get { return _mPfStatus; }
            set
            {
                _mPfStatus = value;
                NotifyPropertyChanged("mPfStatus");
            }
        }

        private string _mPfInfo;
        public string mPfInfo
        {
            get { return _mPfInfo; }
            set
            {
                _mPfInfo = value;
                NotifyPropertyChanged("mPfInfo");
            }
        }

        private string _mPfMem;
        public string mPfMem
        {
            get { return _mPfMem; }
            set
            {
                _mPfMem = value;
                NotifyPropertyChanged("mPfMem");
            }
        }

        private string _mPfTimeout;
        public string mPfTimeout
        {
            get { return _mPfTimeout; }
            set
            {
                _mPfTimeout = value;
                NotifyPropertyChanged("mPfTimeout");
            }
        }

        protected Cache cache;

        Timer timer;
        int refreshTimeout = 10;
        bool timerEventRunning = false;

        public InfoPfMVVM()
        {
            InitializeComponent();

            DataContext = this;

            if (!restoreState())
            {
                refresh();
            }

            timer = new Timer(refreshTimeout * 1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
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

        public void SaveState()
        {
            timer.Stop();

            cache = new InfoPfMVVMCache();

            (cache as InfoPfMVVMCache).mPfStatus = mPfStatus;
            (cache as InfoPfMVVMCache).mPfInfo = mPfInfo;
            (cache as InfoPfMVVMCache).mPfMem = mPfMem;
            (cache as InfoPfMVVMCache).mPfTimeout = mPfTimeout;

            Main.self.cache["InfoPfMVVM"] = cache;
        }

        protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("InfoPfMVVM"))
            {
                cache = Main.self.cache["InfoPfMVVM"] as InfoPfMVVMCache;

                mPfStatus = (cache as InfoPfMVVMCache).mPfStatus;
                mPfInfo = (cache as InfoPfMVVMCache).mPfInfo;
                mPfMem = (cache as InfoPfMVVMCache).mPfMem;
                mPfTimeout = (cache as InfoPfMVVMCache).mPfTimeout;

                return true;
            }
            return false;
        }

        protected void refresh()
        {
            fetchStats();
        }

        private void fetchStats()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                mPfStatus = Main.controller.execute("pf", "IsRunning").status;
                mPfInfo = Main.controller.execute("pf", "GetPfInfo").output;
                mPfMem = Main.controller.execute("pf", "GetPfMemInfo").output;
                mPfTimeout = Main.controller.execute("pf", "GetPfTimeoutInfo").output;

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
    }

    // XXX: Again, these converters are overkill too.
    // These could be simple assignments to the respective vars in a UI update method.
    [ValueConversion(typeof(int), typeof(string))]
    public class StatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var name = parameter as string;
            return (int)value == 0 ? name + " is running" : name + " is not running";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(int), typeof(BitmapImage))]
    public class StatusImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new BitmapImage(new Uri(@"pack://application:,,,/PFFW;component/Images/" + ((int)value == 0 ? "run" : "stop") + ".png", UriKind.Absolute));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class InfoPfMVVMCache : Cache
    {
        public int mPfStatus;
        public string mPfInfo;
        public string mPfMem;
        public string mPfTimeout;
    }
}
