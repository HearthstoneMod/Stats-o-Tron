using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.NetworkInformation;
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
        
        private volatile Dictionary<string, int> Users;
        private volatile Dictionary<string, int> UsersFirstSeen;
        private volatile Dictionary<string, int> UsersLastSeen = new Dictionary<string, int>();
        private volatile Dictionary<string, int> Channels;

        private Role DeveloperRole;
        private Role AdministratorRole;
        private Role ModeratorRole;
        private Role VeteranRole;
        
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

                DeveloperRole = Server.FindRoles("Developers").FirstOrDefault();
                AdministratorRole = Server.FindRoles("Administrators").FirstOrDefault();
                ModeratorRole = Server.FindRoles("Moderators").FirstOrDefault();
                VeteranRole = Server.FindRoles("Veterans").FirstOrDefault();

                LogText("Loaded Stats-o-Tron bot to server " + Server.Name);
            });
        }

        private void LoadFiles()
        {
            if (File.Exists(AppDirectory + "users.list"))
            {
                string usersJson = File.ReadAllText(AppDirectory + "users.list");

                if (usersJson != string.Empty)
                {
                    Users = JsonConvert.DeserializeObject<Dictionary<string, int>>(usersJson);

                    LogText("Loaded " + Users.Count + " users");
                }
                else
                {
                    Users = new Dictionary<string, int>();

                    LogText("Created empty user list");
                }
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

                if (usersfirstseenJson != string.Empty)
                {
                    UsersFirstSeen = JsonConvert.DeserializeObject<Dictionary<string, int>>(usersfirstseenJson);

                    LogText("Loaded " + UsersFirstSeen.Count + " usersfirstseen");
                }
                else
                {
                    UsersFirstSeen = new Dictionary<string, int>();

                    LogText("Created empty user list");
                }
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

                if (usersJson != string.Empty)
                {
                    Channels = JsonConvert.DeserializeObject<Dictionary<string, int>>(usersJson);

                    LogText("Loaded " + Channels.Count + " channels");
                }
                else
                {
                    Channels = new Dictionary<string, int>();

                    LogText("Created empty channel list");
                }
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
            Channel channel = args.Channel;
            User user = args.User;
            string fullUser = user.ToString();

            // Updating channel message count
            if (Channels.ContainsKey(channel.Name))
            {
                Channels[channel.Name]++;
            }

            // Updating user message count
            if (Users.ContainsKey(fullUser))
            {
                Users[fullUser]++;
            }
            else
            {
                Users.Add(fullUser, 1);
            }

            // Updating user last activity
            if (UsersLastSeen.ContainsKey(fullUser))
            {
                UsersLastSeen[fullUser] = DateTime.Now.ToEpoch();
            }
            else
            {
                UsersLastSeen.Add(fullUser, DateTime.Now.ToEpoch());
            }

            if (args.Message.IsAuthor == false)
            {
                if (args.Server.Id == ServerID)
                {
                    string fullText = args.Message.Text;

                    if (fullText.StartsWith("!"))
                    {
                        string[] commands = fullText.Split();
                        bool isDeveloper = user.HasRole(DeveloperRole);
                        bool isAdmin = isDeveloper || user.HasRole(AdministratorRole);
                        bool isModerator = isDeveloper || isAdmin || user.HasRole(ModeratorRole);
                        bool isVeteran = isDeveloper || isAdmin || isModerator || user.HasRole(VeteranRole);

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
                                if (isModerator)
                                {
                                    LogNormalCommand(channel, commands[0], fullUser);
                                    channel.SendMessage("`Latency : " + new Ping().Send("www.discordapp.com").RoundtripTime + " ms`");
                                }
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
                                                        "```!hello - HELLO! (admin+ only)\n" +
                                                        "!ping - Checks bot status (mod+ only)\n" +
                                                        "!help - Shows this message```\n" +

                                                        "**· Stats Commands: **\n" +
                                                        "```!serverstats - Shows the stats of each channel\n" +
                                                        "!usertop - Shows the top 10 users\n" +
                                                        "!usertop <quantity> - Shows the top x users (admin+ only)\n" +
                                                        "!userfirstseentop - Shows the top first seen 10 users\n" +
                                                        "!userfirstseentop <quantity> - Shows the top first seen x users (admin+ only)\n" +
                                                        "!firstseen <fullname> - Checks first activity of someone\n" +
                                                        "!lastseen <fullname> - Checks last activity of someone\n" +
                                                        "!recount - Forces message recount (dev+ only)\n" +
                                                        "!save - Forces data save (dev+ only)```\n");
                                }
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
                                if (isDeveloper)
                                {
                                    LogAdminCommand(channel, commands[0], fullUser);
                                    RecountCommand(channel);
                                }
                                break;

                            case "!firstseen":
                                if (commands.Length > 1)
                                {
                                    LogNormalCommand(channel, commands[0], fullUser);
                                    FirstSeenCommand(channel, commands[1]);
                                }
                                break;

                            case "!lastseen":
                                if (commands.Length > 1)
                                {
                                    LogNormalCommand(channel, commands[0], fullUser);
                                    LastSeenCommand(channel, commands[1]);
                                }
                                break;

                            case "!save":
                                if (isDeveloper)
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

        #region Stats Related Methods

        public async void RecountCommand(Channel channel)
        {
            IEnumerable<string> serverNames = Server.TextChannels.Select(c => c.Name);

            await channel.SendMessage("**Updating server stats...** (" + Server.TextChannels.Count() + " => " + string.Join(", ", serverNames) + ")");

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

                if (downloadedMessages.Length > 0)
                {
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
                
                SaveChannelStatsFile();
                SaveUserStatsFile();
                SaveUserFirstSeenFile();

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
            if (UsersLastSeen.ContainsKey(fullUser))
            {
                channel.SendMessage(fullUser + " was last activity was at " + UsersLastSeen[fullUser].FromEpoch());
            }
            else
            {
                channel.SendMessage(fullUser + " activity was not found");
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
