using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameSpyEmulator
{
    public static class HostsFixer
    {
        public static void Fix(string ip)
        {
            string entries = $@"
{ip} ocs.thq.com
{ip} www.dawnofwargame.com
{ip} gmtest.master.gamespy.com

{ip} whamdowfr.master.gamespy.com
{ip} whamdowfr.gamespy.com
{ip} whamdowfr.ms9.gamespy.com
{ip} whamdowfr.ms11.gamespy.com
{ip} whamdowfr.available.gamespy.com
{ip} whamdowfr.available.gamespy.com
{ip} whamdowfr.natneg.gamespy.com
{ip} whamdowfr.natneg0.gamespy.com
{ip} whamdowfr.natneg1.gamespy.com
{ip} whamdowfr.natneg2.gamespy.com
{ip} whamdowfr.natneg3.gamespy.com
{ip} whamdowfr.gamestats.gamespy.com

{ip} whamdowfram.master.gamespy.com
{ip} whamdowfram.gamespy.com
{ip} whamdowfram.ms9.gamespy.com
{ip} whamdowfram.ms11.gamespy.com
{ip} whamdowfram.available.gamespy.com
{ip} whamdowfram.available.gamespy.com
{ip} whamdowfram.natneg.gamespy.com
{ip} whamdowfram.natneg0.gamespy.com
{ip} whamdowfram.natneg1.gamespy.com
{ip} whamdowfram.natneg2.gamespy.com
{ip} whamdowfram.natneg3.gamespy.com
{ip} whamdowfram.gamestats.gamespy.com

{ip} gamespy.net
{ip} gamespygp
{ip} motd.gamespy.com
{ip} peerchat.gamespy.com
{ip} gamestats.gamespy.com
{ip} gpcm.gamespy.com
{ip} gpsp.gamespy.com
{ip} key.gamespy.com
{ip} master.gamespy.com
{ip} master0.gamespy.com
{ip} natneg.gamespy.com
{ip} natneg0.gamespy.com
{ip} natneg1.gamespy.com
{ip} natneg2.gamespy.com
{ip} natneg3.gamespy.com
{ip} chat.gamespynetwork.com
{ip} available.gamespy.com
{ip} gamespy.com
{ip} gamespyarcade.com
{ip} www.gamespy.com
{ip} www.gamespyarcade.com
{ip} chat.master.gamespy.com
{ip} thq.vo.llnwd.net
{ip} gamespyid.com
{ip} nat.gamespy.com
";

            if (string.IsNullOrWhiteSpace(ip))
                return;

            ModifyHostsFile(entries.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(' ')).Where(x => x.Length == 2).ToList());
        }

        static void ModifyHostsFile(List<string[]> entries)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");

            if (!File.Exists(path))
                File.Create(path);

            var list = new List<string>();

            using (var reader = File.OpenText(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var commentStart = line.IndexOf("#");

                    string[] parts;

                    if (commentStart != -1)
                    {
                        parts = line.Substring(0, commentStart).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    }

                    if (parts.Length != 2)
                    {
                        list.Add(line);
                        continue;
                    }

                    var hostName = parts[1];
                    var address = parts[0];

                    var entry = entries.FirstOrDefault(x => x[1] == hostName);

                    if (entry != null)
                    {
                        entries.Remove(entry);

                        if (entry[0] == address)
                        {
                            list.Add(line);
                        }
                        else
                        {
                            list.Add(line.Replace(address, entry[0]));
                        }
                    }
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    list.Add(entry[0] + " " + entry[1]);
                }
            }

            using (var stream = File.Create(path))
            {
                using (var writer = new StreamWriter(stream))
                {
                    for (int i = 0; i < list.Count; i++)
                        writer.WriteLine(list[i]);
                }
            }
        }
    }
}

