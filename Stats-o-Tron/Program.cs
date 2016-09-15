using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using Newtonsoft.Json;
using Discord;

namespace Stats_o_Tron
{
    public class Program
    {
        #region Constructor 

        private static Program MainProgram;

        static void Main(string[] args)
        {
            MainProgram = new Program();
            MainProgram.Start();
        }

        #endregion

        private const ulong ServerID = 194099156988461056;

        private DiscordClient Client;
        private Server Server;

        private string AppDirectory;

        private List<string> Admins = new List<string>();
        private volatile Dictionary<string, int> Users;
        private volatile Dictionary<string, int> UsersFirstSeen; 
        private volatile Dictionary<string, int> Channels;

        private int RecountCount = 0;
        private Channel RecountChannel;

        public void Start()
        {
            AppDirectory = AppDomain.CurrentDomain.BaseDirectory;

            LoadFiles();

            Client = new DiscordClient();

            Client.MessageReceived += async (obj, args) =>
            {
                await Task.Run(() => ProcessMessageReceived(args));
            };

            Client.MessageDeleted += async (obj, args) =>
            {
                await Task.Run(() => ProcessMessageDeleted(args));
            };

            Client.ExecuteAndWait(async () =>
            {
                await Client.Connect("Bot MjA5MjY1NzQ2MzMxNTY2MDgw.Cn9taQ.MixAdiHpSVNX7N-L69MHodjOM3M");

                await Task.Delay(1000);

                Client.SetGame("Modstone");

                Server = Client.Servers.First(s => s.Id == ServerID);

                LogText("Loaded Stats-o-Tron bot to server " + Server.Name);
            });
        }

        private void LoadFiles()
        {
            if (File.Exists(AppDirectory + "admins.list"))
            {
                string[] admins = File.ReadAllText(AppDirectory + "admins.list").Split(new string[1] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string admin in admins)
                {
                    Admins.Add(admin);
                }

                LogText("Loaded " + admins.Length + " admins");
            }
            else
            {
                File.Create(AppDirectory + "admins.list").Close();

                LogText("Created empty admin list");
            }
            
            if (File.Exists(AppDirectory + "users.list"))
            {
                string usersJson = File.ReadAllText(AppDirectory + "users.list");

                Users = JsonConvert.DeserializeObject<Dictionary<string, int>>(usersJson);

                LogText("Loaded " + Users.Count + " users");
            }
            else
            {
                File.Create(AppDirectory + "users.list").Close();

                Users = new Dictionary<string, int>();

                LogText("Created empty user list");
            }
            
            if (File.Exists(AppDirectory + "usersfirstseen.list"))
            {
                string usersfirstseenJson = File.ReadAllText(AppDirectory + "usersfirstseen.list");

                UsersFirstSeen = JsonConvert.DeserializeObject<Dictionary<string, int>>(usersfirstseenJson);

                LogText("Loaded " + UsersFirstSeen.Count + " usersfirstseen");
            }
            else
            {
                File.Create(AppDirectory + "usersfirstseen.list").Close();

                UsersFirstSeen = new Dictionary<string, int>();

                LogText("Created empty user list");
            }

            if (File.Exists(AppDirectory + "channels.list"))
            {
                string usersJson = File.ReadAllText(AppDirectory + "channels.list");

                Channels = JsonConvert.DeserializeObject<Dictionary<string, int>>(usersJson);

                LogText("Loaded " + Channels.Count + " channels");
            }
            else
            {
                File.Create(AppDirectory + "channels.list").Close();

                Channels = new Dictionary<string, int>();

                LogText("Created empty channel list");
            }

            LogText(" ");
        }

        private void ProcessMessageReceived(MessageEventArgs args)
        {
            string fullUser = args.User.ToString();
            Channel channel = args.Channel;

            if (Channels.ContainsKey(channel.Name))
            {
                Channels[channel.Name]++;
            }

            if (Users.ContainsKey(fullUser))
            {
                Users[fullUser]++;
            }
            else
            {
                Users.Add(fullUser, 1);
            }

            if (args.Message.IsAuthor == false)
            {
                if (args.Server?.Id == ServerID)
                {
                    string fullText = args.Message.Text;

                    if (fullText.StartsWith("!"))
                    {
                        string[] commands = fullText.Split();
                        bool isAdmin = Admins.Contains(fullUser);
                        
                        switch (commands[0].ToLower())
                        {
                            case "!hello":
                                if (isAdmin)
                                {
                                    LogAdminCommand(channel, commands[0], fullUser);
                                    channel.SendTTSMessage("***HELLO! HELLO! HELLO!***");
                                }
                                break;

                            case "!ping":
                                LogNormalCommand(channel, commands[0], fullUser);
                                channel.SendMessage("`Latency : " + new Ping().Send("www.discordapp.com").RoundtripTime + " ms`");
                                break;

                            case "!help":
                                if (commands.Length == 1)
                                {
                                    channel.SendMessage("Use `!help stats` to get the full list of Stats-o-Tron commands");
                                }
                                else if (commands[1].ToLower() == "stats")
                                {
                                    LogNormalCommand(channel, commands[0], fullUser);
                                    channel.SendMessage("**· Normal Commands :**\n " +
                                                        "```!hello - HELLO! (admin only)\n" +
                                                        "!ping - Checks bot status\n" +
                                                        "!help - Shows this message```\n" +

                                                        "**· Admin Commands: **\n" +
                                                        "```!addadmin <fullname> - Adds an admin to the admin list (admin only)\n" +
                                                        "!removeadmin <fullname> - Removes an admin from the admin list (admin only)\n" +
                                                        "!adminlist - Shows the full list of admins```\n" +

                                                        "**· Stats Commands: **\n" +
                                                        "```!serverstats - Shows the stats of each channel\n" +
                                                        "!usertop - Shows the top 10 users\n" +
                                                        "!usertop <quantity> - Shows the top x users (admin only)\n" +
                                                        "!recount - Forces message recount (admin only)\n" +
                                                        "!lastseen <fullname> - Checks last activity of someone (admin only)```\n");
                                }
                                break;
                            case "!addadmin":
                                if (commands.Length > 1 && isAdmin)
                                {
                                    LogAdminCommand(channel, commands[0], fullUser);
                                    AddAdminCommand(channel, commands[1]);
                                }
                                break;

                            case "!removeadmin":
                                if (commands.Length > 1 && isAdmin)
                                {
                                    LogAdminCommand(channel, commands[0], fullUser);
                                    RemoveAdminCommand(channel, commands[1]);
                                }
                                break;

                            case "!adminlist":
                                LogNormalCommand(channel, commands[0], fullUser);
                                ShowAdminListCommand(channel);
                                break;

                            case "!serverstats":
                                LogNormalCommand(channel, commands[0], fullUser);
                                ShowServerStatsCommand(channel);
                                break;

                            case "!usertop":
                                LogNormalCommand(channel, commands[0], fullUser);
                                if (commands.Length > 1 && isAdmin)
                                {
                                    int quantity;

                                    bool succeeded = int.TryParse(commands[1], out quantity);

                                    if (succeeded)
                                    {
                                        ShowUserTopCommand(channel, quantity);
                                    }
                                    else
                                    {
                                        channel.SendMessage("**ERROR : ** Could not parse " + commands[1]);
                                    }
                                }
                                else
                                {
                                    ShowUserTopCommand(channel, 10);
                                }
                                break;

                            case "!userfirstseentop":
                                LogNormalCommand(channel, commands[0], fullUser);
                                if (commands.Length > 1 && isAdmin)
                                {
                                    int quantity;

                                    bool succeeded = int.TryParse(commands[1], out quantity);

                                    if (succeeded)
                                    {
                                        ShowUserFirstSeenTopCommand(channel, quantity);
                                    }
                                    else
                                    {
                                        channel.SendMessage("**ERROR : ** Could not parse " + commands[1]);
                                    }
                                }
                                else
                                {
                                    ShowUserFirstSeenTopCommand(channel, 10);
                                }
                                break;

                            case "!recount":
                                if (isAdmin)
                                {
                                    LogAdminCommand(channel, commands[0], fullUser);
                                    RecountCommand(channel);
                                }
                                break;

                            case "!firstseen":
                                if (commands.Length > 1)
                                {
                                    LogAdminCommand(channel, commands[0], fullUser);
                                    FirstSeenCommand(channel, commands[1]);
                                }
                                break;

                            case "!lastseen":
                                if (commands.Length > 1 && isAdmin)
                                {
                                    LogAdminCommand(channel, commands[0], fullUser);
                                    LastSeenCommand(channel, commands[1]);
                                }
                                break;

                            case "!save":
                                if (isAdmin)
                                {
                                    LogAdminCommand(channel, commands[0], fullUser);
                                    SaveCommand(channel);
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void ProcessMessageDeleted(MessageEventArgs args)
        {
            string fullUser = args.User.ToString();
            Channel channel = args.Channel;

            if (Users.ContainsKey(fullUser))
            {
                Users[fullUser]--;
            }

            if (Channels.ContainsKey(channel.Name))
            {
                Channels[channel.Name]--;
            }
        }

        #region Admin Related Methods

        private void ShowAdminListCommand(Channel channel)
        {
            if (Admins.Count > 0)
            {
                string adminList = "**Showing current admin list **(" + DateTime.Today.ToShortDateString() + ")** :**\n\n```";

                for (int i = 0; i < Admins.Count; i++)
                {
                    adminList += "· " + Admins[i] + "\n";
                }

                channel.SendMessage(adminList + "```");
            }
            else
            {
                channel.SendMessage("**Admin list is empty.**");
            }
        }

        private void AddAdminCommand(Channel channel, string admin)
        {
            if (Server.Users.Any(u => u.ToString() == admin))
            {
                if (Admins.Contains(admin))
                {
                    channel.SendMessage("@" + admin + "** is already an admin.**");
                }
                else
                {
                    AddAdmin(admin);
                    channel.SendMessage("@" + admin + "** was added to the admin list.**");
                }
            }
            else
            {
                channel.SendMessage(admin + "** was not found in the server.**");
            }
        }

        private void RemoveAdminCommand(Channel channel, string admin)
        {
            if (Admins.Contains(admin))
            {
                RemoveAdmin(admin);
                channel.SendMessage(admin + "** was removed from admins.**");
            }
            else
            {
                channel.SendMessage(admin + "** is not an admin.**");
            }
        }

        private void AddAdmin(string admin)
        {
            Admins.Add(admin);
            SaveAdminFile();
        }

        private void RemoveAdmin(string admin)
        {
            Admins.Remove(admin);
            SaveAdminFile();
        }

        private void SaveAdminFile()
        {
            string adminString = string.Join(";", Admins.ToArray());

            if (adminString.StartsWith(";"))
            {
                adminString = adminString.Substring(1);
            }

            File.WriteAllText(AppDirectory + "admins.list", adminString);
        }

        #endregion

        #region Stats Related Methods

        public async void RecountCommand(Channel channel)
        {
            await channel.SendMessage("**Updating server stats...**");

            Users.Clear();
            Channels.Clear();

            RecountCount = 0;
            RecountChannel = channel;
            
            foreach (Channel serverChannel in Server.TextChannels)
            {
                ParameterizedThreadStart threadStart = new ParameterizedThreadStart(DownloadChannelInfo);

                Thread downloadThread = new Thread(threadStart);
                downloadThread.Priority = ThreadPriority.Highest;
                downloadThread.Start(new object[] {serverChannel, channel});
            }
        }

        private async void DownloadChannelInfo(object args)
        {
            object[] argsArray = (object[]) args;
            Channel channel = (Channel) argsArray[0];
            Channel messageChannel = (Channel) argsArray[1];

            Stopwatch timer = new Stopwatch();
            timer.Start();

            int messageCount = 0;
            
            ulong? previousID = channel.Messages.FirstOrDefault()?.Id;
            bool isDone = false;

            while (isDone == false)
            {
                Message[] downloadedMessages = await channel.DownloadMessages(100, previousID, Relative.Before, false);
                
                previousID = downloadedMessages[downloadedMessages.Length - 1].Id;

                foreach (Message message in downloadedMessages)
                {
                    if (message.User != null)
                    {
                        string userName = message.User.ToString();

                        if (Users.ContainsKey(userName))
                        {
                            Users[userName]++;
                            int lastEpoch = UsersFirstSeen[userName];
                            int thisEpoch = message.Timestamp.ToEpoch();

                            UsersFirstSeen[userName] = Math.Min(lastEpoch, thisEpoch);
                        }
                        else
                        {
                            Users.Add(userName, 1);
                            UsersFirstSeen.Add(userName, message.Timestamp.ToEpoch());
                        }
                    }
                }

                messageCount += downloadedMessages.Length;

                if (downloadedMessages.Length < 100)
                {
                    isDone = true;
                }
            }

            Channels.Add(channel.Name, messageCount);

            timer.Stop();

            await messageChannel.SendMessage("**Updated** #" + channel.Name + "** channel info in **" + Math.Round(timer.ElapsedMilliseconds / 1000f, 4) + " s");

            RecountCheckpoint();
        }

        private void RecountCheckpoint()
        {
            RecountCount++;

            if (RecountCount == Server.TextChannels.Count())
            {
                ShowServerStatsCommand(RecountChannel);

                RecountCount = 0;
                RecountChannel = null;
            }
        }

        public void ShowUserTopCommand(Channel channel, int quantity)
        {
            string channelList = "**Showing user top " + quantity + " :**\n```";

            Dictionary<string, int> top = Users.OrderByDescending(x => x.Value).Take(quantity).ToDictionary(u => u.Key, u => u.Value);

            foreach (KeyValuePair<string, int> pair in top)
            {
                channelList += "· " + pair.Key + " -> " + pair.Value + " messages\n";
            }

            channel.SendMessage(channelList + "```");
        }

        public void ShowUserFirstSeenTopCommand(Channel channel, int quantity)
        {
            string channelList = "**Showing user first seen top " + quantity + " :**\n```";

            Dictionary<string, int> top = UsersFirstSeen.OrderBy(x => x.Value).Take(quantity).ToDictionary(u => u.Key, u => u.Value);

            foreach (KeyValuePair<string, int> pair in top)
            {
                channelList += "· " + pair.Key + " -> " + pair.Value.FromEpoch().ToString("G") + "\n";
            }

            channel.SendMessage(channelList + "```");
        }

        public void ShowServerStatsCommand(Channel channel)
        {
            string channelList = "**Showing server stats :**\n```";

            int totalMessages = 0;

            foreach (KeyValuePair<string, int> keyValue in Channels)
            {
                totalMessages += keyValue.Value;
                channelList += "· " + keyValue.Key + " -> " + keyValue.Value + " messages\n";
            }

            channelList += "\n TOTAL : " + totalMessages;

            channel.SendMessage(channelList + "```");
        }

        private void FirstSeenCommand(Channel channel, string fullUser)
        {
            if (UsersFirstSeen.ContainsKey(fullUser))
            {
                channel.SendMessage(fullUser + " was first seen online at " + UsersFirstSeen[fullUser].FromEpoch().ToString("G"));
            }
            else
            {
                channel.SendMessage(fullUser + " was not found");
            }
        }

        private void LastSeenCommand(Channel channel, string fullUser)
        {
            User user = Server.FindUsers(fullUser).FirstOrDefault();

            if (user != null)
            {
                if (user.LastOnlineAt != null && user.LastActivityAt != null)
                {
                    channel.SendMessage(fullUser + " was last seen online at " + user.LastOnlineAt + " and his last activity was at " + user.LastActivityAt);
                }
                else
                {
                    channel.SendMessage(fullUser + " activity was not found");
                }
            }
            else
            {
                channel.SendMessage(fullUser + " was not found");
            }
        }

        private void SaveCommand(Channel channel)
        {
            SaveChannelStatsFile();
            SaveUserStatsFile();
            SaveUserFirstSeenFile();

            channel.SendMessage("**Channel and User data saved**");
        }

        private void SaveChannelStatsFile()
        {
            string channelString = JsonConvert.SerializeObject(Channels);

            File.WriteAllText(AppDirectory + "channels.list", channelString);
        }

        private void SaveUserStatsFile()
        {
            string userString = JsonConvert.SerializeObject(Users);

            File.WriteAllText(AppDirectory + "users.list", userString);
        }

        private void SaveUserFirstSeenFile()
        {
            string userFirstSeenString = JsonConvert.SerializeObject(UsersFirstSeen);

            File.WriteAllText(AppDirectory + "usersfirstseen.list", userFirstSeenString);
        }

        #endregion

        #region Log Methods

        public void LogText(string text)
        {
            Console.WriteLine("<white>" + text + "</white>");
        }

        public void LogNormalCommand(Channel channel, string cmd, string user)
        {
            Console.WriteLine("<cyan>" + cmd + " requested in #" + channel.Name + " by " + user + "</cyan>");
        }

        public void LogAdminCommand(Channel channel, string cmd, string user)
        {
            Console.WriteLine("<green>" + cmd + " requested in #" + channel.Name + " by " + user + "</green>");
        }

        #endregion
    }

    public static class Util
    {
        public static DateTime FromEpoch(this int date)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(date);
        }

        public static int ToEpoch(this DateTime date)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt32((date - epoch).TotalSeconds);
        }
    }
}
