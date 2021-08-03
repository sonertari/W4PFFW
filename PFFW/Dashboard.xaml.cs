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
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PFFW
{
    /// <summary>
    /// Dashboard provides system service status.
    /// </summary>
    public partial class Dashboard : InfoBase
    {
        private string mServiceStatus;
        protected JObject jsonStatus = new JObject();

        protected class ServiceStatusFields
        {
            public Label running;
            public Ellipse runningEllipse;
            public Label error;
            public Ellipse errorEllipse;
            public Label status;

            public ServiceStatusFields(Label r, Ellipse re, Label e, Ellipse ee, Label s)
            {
                running = r;
                runningEllipse = re;
                error = e;
                errorEllipse = ee;
                status = s;
            }
        }

        Dictionary<string, ServiceStatusFields> serviceStatusFields;

        override protected void init()
        {
            InitializeComponent();

            serviceStatusFields = new Dictionary<string, ServiceStatusFields> {
                { "system", new ServiceStatusFields(systemRunning, systemRunningEllipse, systemError, systemErrorEllipse, systemStatus) },
                { "pf", new ServiceStatusFields(pfRunning, pfRunningEllipse, pfError, pfErrorEllipse, pfStatus) },
                { "dhcpd", new ServiceStatusFields(dhcpdRunning, dhcpdRunningEllipse, dhcpdError, dhcpdErrorEllipse, dhcpdStatus) },
                { "dnsmasq", new ServiceStatusFields(dnsmasqRunning, dnsmasqRunningEllipse, dnsmasqError, dnsmasqErrorEllipse, dnsmasqStatus) },
                { "openssh", new ServiceStatusFields(opensshRunning, opensshRunningEllipse, opensshError, opensshErrorEllipse, opensshStatus) },
                { "ftp-proxy", new ServiceStatusFields(ftpproxyRunning, ftpproxyRunningEllipse, ftpproxyError, ftpproxyErrorEllipse, ftpproxyStatus) },
                { "httpd", new ServiceStatusFields(httpdRunning, httpdRunningEllipse, httpdError, httpdErrorEllipse, httpdStatus) },
                { "symon", new ServiceStatusFields(symonRunning, symonRunningEllipse, symonError, symonErrorEllipse, symonStatus) },
                { "symux", new ServiceStatusFields(symuxRunning, symuxRunningEllipse, symuxError, symuxErrorEllipse, symuxStatus) },
                { "sslproxy", new ServiceStatusFields(sslproxyRunning, sslproxyRunningEllipse, sslproxyError, sslproxyErrorEllipse, sslproxyStatus) },
                { "e2guardian", new ServiceStatusFields(e2guardianRunning, e2guardianRunningEllipse, e2guardianError, e2guardianErrorEllipse, e2guardianStatus) },
                { "snort", new ServiceStatusFields(snortRunning, snortRunningEllipse, snortError, snortErrorEllipse, snortStatus) },
                { "snortinline", new ServiceStatusFields(snortinlineRunning, snortinlineRunningEllipse, snortinlineError, snortinlineErrorEllipse, snortinlineStatus) },
                { "snortips", new ServiceStatusFields(snortipsRunning, snortipsRunningEllipse, snortipsError, snortipsErrorEllipse, snortipsStatus) },
                { "spamassassin", new ServiceStatusFields(spamassassinRunning, spamassassinRunningEllipse, spamassassinError, spamassassinErrorEllipse, spamassassinStatus) },
                { "clamd", new ServiceStatusFields(clamdRunning, clamdRunningEllipse, clamdError, clamdErrorEllipse, clamdStatus) },
                { "freshclam", new ServiceStatusFields(freshclamRunning, freshclamRunningEllipse, freshclamError, freshclamErrorEllipse, freshclamStatus) },
                { "p3scan", new ServiceStatusFields(p3scanRunning, p3scanRunningEllipse, p3scanError, p3scanErrorEllipse, p3scanStatus) },
                { "smtp-gated", new ServiceStatusFields(smtpgatedRunning, smtpgatedRunningEllipse, smtpgatedError, smtpgatedErrorEllipse, smtpgatedStatus) },
                { "imspector", new ServiceStatusFields(imspectorRunning, imspectorRunningEllipse, imspectorError, imspectorErrorEllipse, imspectorStatus) },
                { "openvpn", new ServiceStatusFields(openvpnRunning, openvpnRunningEllipse, openvpnError, openvpnErrorEllipse, openvpnStatus) },
                { "dante", new ServiceStatusFields(danteRunning, danteRunningEllipse, danteError, danteErrorEllipse, danteStatus) },
                { "spamd", new ServiceStatusFields(spamdRunning, spamdRunningEllipse, spamdError, spamdErrorEllipse, spamdStatus) },
                { "collectd", new ServiceStatusFields(collectdRunning, collectdRunningEllipse, collectdError, collectdErrorEllipse, collectdStatus) },
        };
        }

        override public void SaveState()
        {
            base.SaveState();

            cache = new DashboardCache();

            (cache as DashboardCache).mServiceStatus = mServiceStatus;

            Main.self.cache["Dashboard"] = cache;
        }

        override protected bool restoreState()
        {
            if (Main.self.cache.ContainsKey("Dashboard"))
            {
                cache = Main.self.cache["Dashboard"] as DashboardCache;

                mServiceStatus = (cache as DashboardCache).mServiceStatus;
                jsonStatus = JsonConvert.DeserializeObject<JObject>(mServiceStatus);

                updateView();

                return true;
            }
            return false;
        }

        override protected void fetch()
        {
            mServiceStatus = Main.controller.execute("system", "GetServiceStatus").output;
            jsonStatus = JsonConvert.DeserializeObject<JObject>(mServiceStatus).GetValue("status") as JObject;

            var strReloadRate = Main.controller.execute("pf", "GetReloadRate").output;

            int timeout = int.Parse(strReloadRate);
            refreshTimeout = timeout < 10 ? 10 : timeout;
        }

        override protected void updateView()
        {
            int critical = 0;
            int error = 0;
            int warning = 0;

            var it = (jsonStatus as JObject).GetEnumerator();
            while (it.MoveNext())
            {
                string key = it.Current.Key;
                if (!serviceStatusFields.ContainsKey(key))
                {
                    continue;
                }

                serviceStatusFields[key].running.Content = jsonStatus[key]["Status"];
                if (serviceStatusFields[key].running.Content.ToString().Equals("R"))
                {
                    serviceStatusFields[key].runningEllipse.Fill = new SolidColorBrush(Colors.Blue);
                    serviceStatusFields[key].running.Foreground = new SolidColorBrush(Colors.White);
                }
                else
                {
                    serviceStatusFields[key].runningEllipse.Fill = new SolidColorBrush(Colors.Gray);
                    serviceStatusFields[key].running.Foreground = new SolidColorBrush(Colors.Black);
                }

                serviceStatusFields[key].error.Content = jsonStatus[key]["ErrorStatus"];
                if (serviceStatusFields[key].error.Content.ToString().Equals("C"))
                {
                    serviceStatusFields[key].errorEllipse.Fill = new SolidColorBrush(Colors.Red);
                    serviceStatusFields[key].error.Foreground = new SolidColorBrush(Colors.White);
                }
                else if (serviceStatusFields[key].error.Content.ToString().Equals("E"))
                {
                    serviceStatusFields[key].errorEllipse.Fill = new SolidColorBrush(Colors.Orange);
                    serviceStatusFields[key].error.Foreground = new SolidColorBrush(Colors.White);
                }
                else if (serviceStatusFields[key].error.Content.ToString().Equals("W"))
                {
                    serviceStatusFields[key].errorEllipse.Fill = new SolidColorBrush(Colors.Yellow);
                    serviceStatusFields[key].error.Foreground = new SolidColorBrush(Colors.Black);
                }
                else
                {
                    serviceStatusFields[key].errorEllipse.Fill = new SolidColorBrush(Colors.LimeGreen);
                    serviceStatusFields[key].error.Foreground = new SolidColorBrush(Colors.Black);
                }

                int c = int.Parse(jsonStatus[key]["Critical"].ToString());
                int e = int.Parse(jsonStatus[key]["Error"].ToString());
                int w = int.Parse(jsonStatus[key]["Warning"].ToString());

                serviceStatusFields[key].status.Content = "";
                if (c > 0)
                {
                    critical += c;
                    serviceStatusFields[key].status.Content = "Critical: " + c;
                }

                if (e > 0)
                {
                    error += e;
                    if (c > 0)
                    {
                        serviceStatusFields[key].status.Content = ", ";
                    }
                    serviceStatusFields[key].status.Content = "Error: " + e;
                }

                if (w > 0)
                {
                    warning += w;
                    if (c > 0 || e > 0)
                    {
                        serviceStatusFields[key].status.Content = ", ";
                    }
                    serviceStatusFields[key].status.Content = "Warning: " + w;
                }
            }

            criticalNumber.Content = critical;
            errorNumber.Content = error;
            warningNumber.Content = warning;
        }
    }

    public class DashboardCache : Cache
    {
        public string mServiceStatus;
    }
}
