﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
        private Dictionary<string, int> Users; 
        private Dictionary<string, int> Channels;

        public void Start()
        {
            AppDirectory = AppDomain.CurrentDomain.BaseDirectory;

            LoadFiles();

            Client = new DiscordClient();

            Client.MessageReceived += async (obj, args) =>
            {
                await Task.Run(() => ProcessMessage(args));
            };

            Client.ExecuteAndWait(async () =>
            {
                await Client.Connect("MjA5MjY1NzQ2MzMxNTY2MDgw.Cn9taQ.MixAdiHpSVNX7N-L69MHodjOM3M");

                await Task.Delay(1000);

                Server = Client.Servers.First(s => s.Id == ServerID);

                Console.WriteLine("Loaded Stats-o-Tron bot to server " + Server.Name);
            });
        }

        private void LoadFiles()
        {
            // Admin files

            if (File.Exists(AppDirectory + "admins.list"))
            {
                string[] admins = File.ReadAllText(AppDirectory + "admins.list").Split(new string[1] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                Console.WriteLine("Loading admins (" + admins.Length + ") :");

                foreach (string admin in admins)
                {
                    Admins.Add(admin);
                    Console.WriteLine("· " + admin);
                }
            }
            else
            {
                File.Create(AppDirectory + "admins.list").Close();

                Console.WriteLine("Created empty admin list");
            }

            Console.WriteLine(" ");

            // User files

            if (File.Exists(AppDirectory + "users.list"))
            {
                string usersJson = File.ReadAllText(AppDirectory + "users.list");

                Users = JsonConvert.DeserializeObject<Dictionary<string, int>>(usersJson);

                Console.WriteLine("Loaded " + Users.Count + " users");
            }
            else
            {
                File.Create(AppDirectory + "users.list").Close();

                Users = new Dictionary<string, int>();

                Console.WriteLine("Created empty user list");
            }

            Console.WriteLine(" ");

            // Channel files

            if (File.Exists(AppDirectory + "channels.list"))
            {
                string usersJson = File.ReadAllText(AppDirectory + "channels.list");

                Channels = JsonConvert.DeserializeObject<Dictionary<string, int>>(usersJson);

                Console.WriteLine("Loaded " + Channels.Count + " channels");
            }
            else
            {
                File.Create(AppDirectory + "channels.list").Close();

                Channels = new Dictionary<string, int>();

                Console.WriteLine("Created empty channel list");
            }

            Console.WriteLine(" ");
        }

        private void ProcessMessage(MessageEventArgs args)
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
                                    channel.SendTTSMessage("***HELLO! HELLO! HELLO!***");
                                }
                                break;

                            case "!help":
                                channel.SendMessage("**List of commands available :** \n");

                                channel.SendMessage("**· Normal Commands :**\n " +
                                                    "```!hello - HELLO! (admin only)\n" +
                                                    "!help - Shows this message```\n" +

                                                    "**· Admin Commands: **\n" +
                                                    "```!addadmin <fullname> - Adds an admin to the admin list (admin only)\n" +
                                                    "!removeadmin <fullname> -Removes an admin from the admin list (admin only)\n" +
                                                    "!adminlist - Show the full list of admins```\n");
                                break;
                            case "!addadmin":
                                if (commands.Length > 1 && isAdmin)
                                {
                                    AddAdminCommand(channel, commands[1]);
                                }
                                break;

                            case "!removeadmin":
                                if (commands.Length > 1 && isAdmin)
                                {
                                    RemoveAdminCommand(channel, commands[1]);
                                }
                                break;

                            case "!adminlist":
                                ShowAdminListCommand(channel);
                                break;

                            case "!serverstats":
                                ShowServerStatsCommand(channel);
                                break;

                            case "!usertop":
                                ShowUserTopCommand(channel);
                                break;

                            case "!recount":
                                if (isAdmin)
                                {
                                    RecountCommand(channel);
                                }
                                break;
                        }
                    }
                }
            }
        }

        #region Admin Related Methods

        private void ShowAdminListCommand(Channel channel)
        {
            if (Admins.Count > 0)
            {
                channel.SendMessage("**Showing current admin list **(" + DateTime.Today.ToShortDateString() + ")** :**");

                string adminList = "```";

                for (int i = 0; i < Admins.Count; i++)
                {
                    adminList += "· " + Admins[i] + "\n";
                }

                channel.SendMessage(adminList.Remove(adminList.Length - 3) + " ```");
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
            await channel.SendMessage("**Updating channel stats...**");

            await Task.Delay(1000);

            Users.Clear();
            Channels.Clear();
            
            foreach (Channel serverChannel in Server.TextChannels)
            {
                await DownloadChannelInfo(serverChannel);
            }

            ShowServerStatsCommand(channel);

            SaveChannelStatsFile();
            SaveUserStatsFile();
        }

        private async Task DownloadChannelInfo(Channel channel)
        {
            List<Message> messageList = new List<Message>();
            
            ulong? previousID = channel.Messages.FirstOrDefault()?.Id;
            bool isDone = false;

            while (isDone == false)
            {
                Message[] downloadedMessages = await channel.DownloadMessages(100, previousID, Relative.Before, false);
                
                previousID = downloadedMessages[downloadedMessages.Length - 1].Id;

                messageList = messageList.Concat(downloadedMessages).ToList();

                if (downloadedMessages.Length < 100)
                {
                    isDone = true;
                }
            }

            Channels.Add(channel.Name, messageList.Count);

            foreach (Message message in messageList)
            {
                if (message.User != null)
                {
                    string userName = message.User.ToString();

                    if (Users.ContainsKey(userName))
                    {
                        Users[userName] += 1;
                    }
                    else
                    {
                        Users.Add(userName, 1);
                    }
                }
            }
        }

        public void ShowUserTopCommand(Channel channel)
        {
            channel.SendMessage("**Showing user top 5 :**");

            string channelList = "";

            Dictionary<string, int> top = Users.OrderByDescending(x => x.Value).Take(5).ToDictionary(u => u.Key, u => u.Value);

            foreach (KeyValuePair<string, int> pair in top)
            {
                channelList += "· " + pair.Key + " -> " + pair.Value + " messages\n";
            }

            channel.SendMessage("```" + channelList + "```");
        }

        public void ShowServerStatsCommand(Channel channel)
        {
            channel.SendMessage("**Showing server stats :**");
            
            string channelList = "";

            foreach (KeyValuePair<string, int> keyValue in Channels)
            {
                channelList += "· " + keyValue.Key + " -> " + keyValue.Value + " messages\n";
            }

            channel.SendMessage("```" + channelList + "```");
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

        #endregion
    }
}