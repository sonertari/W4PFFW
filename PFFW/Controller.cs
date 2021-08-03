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
using Renci.SshNet;
using System;
using System.Collections.Generic;

namespace PFFW
{
    public class Controller
    {
        private SshClient ssh;

        private string user;
        private string passwd;
        public string host;
        private int port;

        public string previousHost;

        public string hostname;

        public class CommandOutput
        {
            public string output = "";
            public string error = "";
            public int status = 1;

            public CommandOutput()
            {
            }

            public CommandOutput(string[] arr)
            {
                output = arr[0];
                error = arr[1];
                status = int.Parse(arr[2]);
            }
        }

        public void logOut()
        {
            // Make sure ssh is not null, so we can call logOut() during application start
            if (ssh != null && ssh.IsConnected)
            {
                ssh.Disconnect();
            }

            user = "";
            passwd = "";
            host = "";
            port = 22;
        }

        public bool logIn(string u, string p, string h, int po)
        {
            user = u;
            passwd = p;
            host = h;
            port = po;

            var retval = false;

            // Create a new ssh client everytime logIn() is called
            ssh = new SshClient(host, port, user, passwd);

            if (connect())
            {
                var sshCmd = ssh.CreateCommand(JsonConvert.SerializeObject(new List<string> { "en_EN", "system", "GetMyName" }));
                sshCmd.CommandTimeout = TimeSpan.FromSeconds(10);

                sshCmd.Execute();

                if (sshCmd.ExitStatus == 0)
                {
                    hostname = new CommandOutput(JsonConvert.DeserializeObject<string[]>(sshCmd.Result)).output.TrimEnd();
                    retval = true;
                }
            }
            return retval;
        }

        private bool connect()
        {
            if (!ssh.IsConnected)
            {
                ssh.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
                ssh.ConnectionInfo.RetryAttempts = 3;

                ssh.Connect();
            }
            return ssh.IsConnected;
        }

        public CommandOutput execute(string model, string cmd, params object[] args)
        {
            var command = new List<string> { "en_EN", model, cmd };
            foreach (object a in args)
            {
                command.Add(a.ToString());
            }

            return run(JsonConvert.SerializeObject(command));
        }

        private CommandOutput run(string command)
        {
            var output = new CommandOutput();

            // Reconnect if the session has dropped
            if (connect())
            {
                // Use CreateCommand() method to set the command timeout, do not use RunCommand()
                //var sshCmd = Main.self.ssh.RunCommand(command);
                var sshCmd = ssh.CreateCommand(command);
                sshCmd.CommandTimeout = TimeSpan.FromSeconds(30);

                // TODO: Use async command execution instead? But what do we display until async exec is completed?
                sshCmd.Execute();

                output = new CommandOutput(JsonConvert.DeserializeObject<string[]>(sshCmd.Result));

                // ATTENTION: The exit status of IsRunning command is 1, so we cannot enable the following lines.
                // TODO: Should we try to enable the following to handle error conditions here?

                //var cmdOut = new CommandOutput(JsonConvert.DeserializeObject<string[]>(sshCmd.Result));

                //// sshCmd.ExitStatus and cmdOut.status should be one and the same thing, but check both in any case
                //if (sshCmd.ExitStatus == 0 && cmdOut.status == 0)
                //{
                //    output = cmdOut;

                //    // There may be errors even if the exit status is 0
                //    if (!cmdOut.error.Equals(""))
                //    {
                //        MessageBox.Show("Error: " + cmdOut.error);
                //    }
                //}
                //else
                //{
                //    throw new Exception(cmdOut.error);
                //}
            }
            return output;
        }
    }
}
