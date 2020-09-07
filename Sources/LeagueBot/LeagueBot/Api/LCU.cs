using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Leaf.xNet;
using LeagueBot.Game.Entities;
using LeagueBot.IO;
using System.Diagnostics;

namespace LeagueBot.Api
{
    public class LCU
    {
        public int port;
        public string auth;
        HttpRequest request = new HttpRequest();


        public LCU()
        {
            this.readLockFile();
        }

        private String GetPort()
        {
            var processes = Process.GetProcessesByName("LeagueClient");

            using (var ns = new Process())
            {
                ProcessStartInfo psi = new ProcessStartInfo("netstat.exe", "-ano");
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                ns.StartInfo = psi;
                ns.Start();

                using (StreamReader r = ns.StandardOutput)
                {
                    string output = r.ReadToEnd();
                    ns.WaitForExit();

                    string[] lines = output.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                    foreach (string line in lines)
                    {
                        if (line.Contains(processes[0].Id.ToString()) && line.Contains("0.0.0.0:0"))
                        {
                            var outp = line.Split(' ');
                            return outp[6].Replace("127.0.0.1:", "");
                        }
                    }
                }
            }
            return String.Empty;
        }

        string _port = string.Empty;
        string Port
        {
            get
            {
                if (string.IsNullOrEmpty(_port))
                {
                    return GetPort();
                }

                return _port;
            }
        }

        public void startQueue()
        {
            updateRequest();
            String url = "https://127.0.0.1:" + this.Port + "/lol-lobby/v2/lobby/matchmaking/search";
            request.AddHeader("Authorization", "Basic " + this.auth);
            String response = request.Post(url).ToString();
        }

        public bool leaverbuster()
        {
            try
            {
                updateRequest();
                String url = "https://127.0.0.1:" + this.Port + "/lol-lobby/v2/lobby/matchmaking/search-state";
                request.AddHeader("Authorization", "Basic " + this.auth);
                string response = request.Get(url).ToString();
                return response.Contains("QUEUE_DODGER") || response.Contains("LEAVER_BUSTED");
            }
            catch
            {
                return false;
            }

        }

        public bool inChampSelect()
        {
            try
            {
                string stringUrl = "https://127.0.0.1:" + this.Port + "/lol-champ-select/v1/session";
                updateRequest();
                return request.Get(stringUrl).ToString().Contains("action");
            }
            catch
            {
                return false;
            }
        }

        public void createLobby(string type)
        {
            string id = (type == "intro") ? "830" : "850";
            updateRequest();
            string url = "https://127.0.0.1:" + this.Port + "/lol-lobby/v2/lobby";
            string content = request.Post(url, "{\"queueId\": " + id + "}", "application/json").StatusCode.ToString();
            Console.WriteLine(content);
        }


        public void pickChampion(int ChampionID)
        {
            System.Threading.Thread.Sleep(2500);
            for (int i = 0; i < 10; i++)
            {
                string url = "https://127.0.0.1:" + this.Port + "/lol-champ-select/v1/session/actions/" + i;
                updateRequest();
                string statusCode = request.Patch(url, "{\"actorCellId\": 0, \"championId\": " + ChampionID + ", \"completed\": true, \"id\": " + i + ", \"isAllyAction\": true, \"type\": \"string\"}", "application/json").ToString();
            }
        }

        public void pickChampionByName(string name)
        {
            Champions ch = new Champions();
            this.pickChampion(ch.getIdByChamp(name));
        }

        public void acceptQueue()
        {
            string url = "https://127.0.0.1:" + this.Port + "/lol-matchmaking/v1/ready-check/accept";
            updateRequest();
            HttpResponse result = request.Post(url);
        }

        #region misc

        private void updateRequest()
        {
            this.request = new HttpRequest();
            this.request.AddHeader("Authorization", "Basic " + this.auth);
            this.request.AddHeader("Accept", "application/json");
            this.request.AddHeader("content-type", "application/json");
            this.request.IgnoreProtocolErrors = true;
        }

        public void readLockFile()
        {
            try
            {
                using (var fileStream = new FileStream(Path.Combine(Configuration.Instance.ClientPath, @"League Of Legends\lockfile"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var streamReader = new StreamReader(fileStream, Encoding.Default))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            string[] lines = line.Split(':');
                            string riot_pass = lines[3];
                            this.auth = Convert.ToBase64String(Encoding.UTF8.GetBytes("riot:" + riot_pass));
                        }
                    }
                }
            }
            catch
            {
                Logger.Write("ERROR: lockfile not found. Is the LoL client started? Are you logged in?");
            }

        }
        #endregion
    }
}
