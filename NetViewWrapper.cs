using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Core.Network
{
    /// <summary>
    /// looks up computer names using the net-view command.
    /// </summary>
    public class NetViewWrapper
    {

        public Process NetUtil { get; protected set; } = new Process();

        public List<string> ComputerNames { get; } = new List<string>();

        public bool IsComplete { get; protected set; }

        public event EventHandler ProcessCompleted;

        public Task Process { get; }

        /// <summary>
        /// construct the wrapper and start scanning the network.
        /// </summary>
        public NetViewWrapper()
        {
            this.Process = Task.Run((Action)Exec);
        }

        private void Exec()
        {
            NetUtil = new Process();
            NetUtil.StartInfo.FileName = "net.exe";
            NetUtil.StartInfo.CreateNoWindow = true;
            NetUtil.StartInfo.Arguments = "view";
            NetUtil.StartInfo.RedirectStandardOutput = true;
            NetUtil.StartInfo.UseShellExecute = false;
            NetUtil.StartInfo.RedirectStandardError = true;
            NetUtil.Start();
            using (var reader = new StreamReader(NetUtil.StandardOutput.BaseStream, NetUtil.StandardOutput.CurrentEncoding))
            {
                string line = "";

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("\\", StringComparison.OrdinalIgnoreCase))
                    {
                        var name = line.Substring(2);
                        var idx = name.IndexOf(' ');
                        name = name.Substring(0, idx);

                        ComputerNames.Add(name);
                    }
                }
            }
            NetUtil.WaitForExit();
            ProcessCompleted?.Invoke(this, EventArgs.Empty);
        }

    }
}
