﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Mud.Importer.Mystery;
using Mud.Logger;
using Mud.Network;
using Mud.Network.Socket;
using Mud.POC;
using Mud.Server.Server;

namespace Mud.Server.TestApplication
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Log.Default.Initialize(ConfigurationManager.AppSettings["logpath"], "server.log");

            //TestSecondWindow();
            //TestPaging();
            //TestCommandParsing();
            //TestBasicCommands();
            //TestWorldOnline();
            TestWorldOffline();
        }

        //private static void TestSecondWindow()
        //{
        //    ProcessStartInfo psi = new ProcessStartInfo("cmd.exe")
        //    {
        //        RedirectStandardError = true,
        //        RedirectStandardInput = true,
        //        RedirectStandardOutput = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true,
        //        WindowStyle = ProcessWindowStyle.Normal
        //    };

        //    Process p = Process.Start(psi);

        //    StreamWriter sw = p.StandardInput;
        //    StreamReader sr = p.StandardOutput;

        //    sw.WriteLine("Hello world!");
        //    sr.Close();
        //}

        private static void CreateDummyWorld()
        {
            World.World world = World.World.Instance as World.World;

            IRoom room1 = world.AddRoom(Guid.NewGuid(), "Room1");
            IRoom room2 = world.AddRoom(Guid.NewGuid(), "Room2");
            world.AddExit(room1, room2, ServerOptions.ExitDirections.North, true);

            ICharacter mob1 = world.AddCharacter(Guid.NewGuid(), "Mob1", room1);
            ICharacter mob2 = world.AddCharacter(Guid.NewGuid(), "Mob2", room1);
            ICharacter mob3 = world.AddCharacter(Guid.NewGuid(), "Mob3", room2);
            ICharacter mob4 = world.AddCharacter(Guid.NewGuid(), "Mob4", room2);
            ICharacter mob5 = world.AddCharacter(Guid.NewGuid(), "Mob5", room2);

            // Item1*2 in Room1
            // Item2 in Mob2
            // Item3 in 2.Item1
            // Item4 in Mob1
            // Item1 in Mob1
            IItem item1 = world.AddItemContainer(Guid.NewGuid(), "Item1", room1);
            IItem item1Dup1 = world.AddItemContainer(Guid.NewGuid(), "Item1", room1);
            IItem item2 = world.AddItemContainer(Guid.NewGuid(), "Item2", mob2);
            IItem item3 = world.AddItemContainer(Guid.NewGuid(), "Item3", item1Dup1 as IContainer);
            IItem item4 = world.AddItemContainer(Guid.NewGuid(), "Item4", mob1);
            IItem item1Dup2 = world.AddItemContainer(Guid.NewGuid(), "Item1", mob1);
        }

        private static void CreateMidgaard()
        {
            MysteryImporter importer = new MysteryImporter();
            importer.Load(@"D:\GitHub\OldMud\area\midgaard.are");
            importer.Parse();

            World.World world = World.World.Instance as World.World;

            Dictionary<int, IRoom> roomsByVNums = new Dictionary<int, IRoom>();

            // Create Rooms
            foreach (RoomData importedRoom in importer.Rooms)
            {
                IRoom room = world.AddRoom(importedRoom.Name, importedRoom.Description);
                roomsByVNums.Add(importedRoom.VNum, room);
            }
            // Create Exits
            foreach (RoomData room in importer.Rooms)
                //foreach (ExitData exit in room.Exits.Where(x => x != null))
                for(int i = 0; i < RoomData.MaxExits-1; i++)
                {
                    ExitData exit = room.Exits[i];
                    if (exit != null)
                    {
                        IRoom from; 
                        roomsByVNums.TryGetValue(room.VNum, out from);
                        IRoom to; 
                        roomsByVNums.TryGetValue(exit.DestinationVNum, out to);
                        if (from == null)
                            Log.Default.WriteLine(LogLevels.Error, "Origin room not found for vnum {0}", room.VNum);
                        else if (to == null)
                            Log.Default.WriteLine(LogLevels.Error, "Destination room not found for vnum {0}", room.VNum);
                        else
                        {
                            world.AddExit(from, to, (ServerOptions.ExitDirections)i, false);
                        }
                    }
                }
            // Add dummy mobs and items to allow impersonate :)
            IRoom templeOfMota = world.GetRooms().FirstOrDefault(x => x.Name.ToLower() == "the temple of mota");
            IRoom templeSquare = world.GetRooms().FirstOrDefault(x => x.Name.ToLower() == "the temple square");
            
            ICharacter mob1 = world.AddCharacter(Guid.NewGuid(), "Mob1", templeOfMota);
            ICharacter mob2 = world.AddCharacter(Guid.NewGuid(), "Mob2", templeOfMota);
            ICharacter mob3 = world.AddCharacter(Guid.NewGuid(), "Mob3", templeSquare);
            ICharacter mob4 = world.AddCharacter(Guid.NewGuid(), "Mob4", templeSquare);
            ICharacter mob5 = world.AddCharacter(Guid.NewGuid(), "Mob5", templeSquare);

            // Item1*2 in Room1
            // Item2 in Mob2
            // Item3 in 2.Item1
            // Item4 in Mob1
            // Item1 in Mob1
            IItem item1 = world.AddItemContainer(Guid.NewGuid(), "Item1", templeOfMota);
            IItem item1Dup1 = world.AddItemContainer(Guid.NewGuid(), "Item1", templeOfMota);
            IItem item2 = world.AddItemContainer(Guid.NewGuid(), "Item2", mob2);
            IItem item3 = world.AddItemContainer(Guid.NewGuid(), "Item3", item1Dup1 as IContainer);
            IItem item4 = world.AddItemContainer(Guid.NewGuid(), "Item4", mob1);
            IItem item1Dup2 = world.AddItemContainer(Guid.NewGuid(), "Item1", mob1);
        }

        private static void TestPaging()
        {

            TestPaging paging = new TestPaging();
            paging.SetData(new StringBuilder("1/Lorem ipsum dolor sit amet, " + Environment.NewLine +
                                             "2/consectetur adipiscing elit, " + Environment.NewLine +
                                             "3/sed do eiusmod tempor incididunt " + Environment.NewLine +
                                             "4/ut labore et dolore magna aliqua. " + Environment.NewLine +
                                             "5/Ut enim ad minim veniam, " + Environment.NewLine +
                                             "6/quis nostrud exercitation ullamco " + Environment.NewLine +
                                             "7/laboris nisi ut aliquip ex " + Environment.NewLine +
                                             "8/ea commodo consequat. " + Environment.NewLine +
                                             "9/Duis aute irure dolor in " + Environment.NewLine +
                                             "10/reprehenderit in voluptate velit " + Environment.NewLine +
                                             "11/esse cillum dolore eu fugiat " + Environment.NewLine +
                                             "12/nulla pariatur. " + Environment.NewLine +
                                             "13/Excepteur sint occaecat " + Environment.NewLine +
                                             "14/cupidatat non proident, " + Environment.NewLine +
                                             "15/sunt in culpa qui officia deserunt " + Environment.NewLine +
                                             "16/mollit anim id est laborum."));
            bool hasPaging1 = paging.HasPaging;
            string line1 = paging.GetNextLines(1);
            bool hasPaging2 = paging.HasPaging;
            string line2_10 = paging.GetNextLines(9);
            bool hasPaging3 = paging.HasPaging;
            string line11_19 = paging.GetNextLines(9);
            bool hasPaging4 = paging.HasPaging;
        }

        private static void TestBasicCommands()
        {
            //Server.Instance.Start();
            // !!!! Must be used with ServerOptions.AsynchronousXXX set to true
            IPlayer player1 = Server.Server.Instance.AddPlayer(new ConsoleClient("Player1"), "Player1");
            IPlayer player2 = Server.Server.Instance.AddPlayer(new ConsoleClient("Player2"), "Player2");
            IAdmin admin = Server.Server.Instance.AddAdmin(new ConsoleClient("Admin1"), "Admin1");

            CreateDummyWorld();

            player1.ProcessCommand("impersonate mob1");
            player1.ProcessCommand("order"); // not controlling anyone
            player1.ProcessCommand("charm mob2");
            player1.ProcessCommand("test");
            player1.ProcessCommand("order test");

            player1.ProcessCommand("look");

            player2.ProcessCommand("gossip Hellow :)");
            player2.ProcessCommand("tell player1 Tsekwa =D");

            player2.ProcessCommand("i mob3");
            player2.ProcessCommand("charm mob2"); // not in same room
            player2.ProcessCommand("charm mob3"); // cannot charm itself (player2 is impersonated in mob3)
            player2.ProcessCommand("ch mob4");

            player2.ProcessCommand("look");

            player1.ProcessCommand("say Hello World!");

            player2.ProcessCommand("order charm mob5");

            player2.ProcessCommand("north"); // no exit on north
            player2.ProcessCommand("south");
            player1.ProcessCommand("south"); // no exit on south

            player1.ProcessCommand("say Hello World!");

            player1.ProcessCommand("/who");
            admin.ProcessCommand("who");

            //player1.ProcessCommand("/commands");
            //player1.ProcessCommand("commands");
            //mob1.ProcessCommand("commands");
            //admin.ProcessCommand("commands");
            //Console.ReadLine();
            //Server.Instance.Stop();
        }

        private static void TestCommandParsing()
        {
            // server doesn't need to be started, we are not testing real runtime but basic commands

            World.World world = World.World.Instance as World.World;
            IRoom room = world.AddRoom(Guid.NewGuid(), "Room");

            IPlayer player = Server.Server.Instance.AddPlayer(new ConsoleClient("Player"), "Player");
            player.ProcessCommand("test");
            player.ProcessCommand("test arg1");
            player.ProcessCommand("test 'arg1' 'arg2' 'arg3' 'arg4'");
            player.ProcessCommand("test 'arg1 arg2' 'arg3 arg4'");
            player.ProcessCommand("test 'arg1 arg2\" arg3 arg4");
            player.ProcessCommand("test 3.arg1");
            player.ProcessCommand("test 2.'arg1'");
            player.ProcessCommand("test 2'.arg1'");
            player.ProcessCommand("test 2.'arg1 arg2' 3.arg3 5.arg4");
            player.ProcessCommand("test 2."); // INVALID
            player.ProcessCommand("test ."); // INVALID
            player.ProcessCommand("test '2.arg1'");
            player.ProcessCommand("unknown"); // INVALID
            player.ProcessCommand("/test");

            ICharacter character = world.AddCharacter(Guid.NewGuid(), "Character", room);
            character.ProcessCommand("look");
            character.ProcessCommand("tell"); // INVALID because Player commands are not accessible by Character
            character.ProcessCommand("unknown"); // INVALID

            player.ProcessCommand("impersonate"); // impossible but doesn't cause an error log to un-impersonate, player must already be impersonated
            player.ProcessCommand("impersonate character");
            player.ProcessCommand("/tell");
            player.ProcessCommand("tell"); // INVALID because OOG is not accessible while impersonating unless if command starts with /
            player.ProcessCommand("look");

            player.ProcessCommand("impersonate"); // INVALID because OOG is not accessible while impersonating unless if command starts with /
            player.ProcessCommand("/impersonate");
            player.ProcessCommand("/tell");
            player.ProcessCommand("tell");
            player.ProcessCommand("look"); // INVALID because Character commands are not accessible by Player unless if impersonating

            IAdmin admin = Server.Server.Instance.AddAdmin(new ConsoleClient("Admin"), "Admin");
            admin.ProcessCommand("incarnate");
            admin.ProcessCommand("unknown"); // INVALID
        }

        private static void TestImport()
        {
            MysteryImporter importer = new MysteryImporter();
            importer.Load(@"D:\GitHub\OldMud\area\midgaard.are");
            importer.Parse();

            //string path = @"D:\GitHub\OldMud\area";
            //string fileList = Path.Combine(path, "area.lst");
            //string[] areaFilenames = File.ReadAllLines(fileList);
            //foreach (string areaFilename in areaFilenames)
            //{
            //    string areaFullName = Path.Combine(path, areaFilename);
            //    MysteryImporter importer = new MysteryImporter();
            //    importer.Load(areaFullName);
            //    importer.Parse();
            //}
        }

        private static void TestWorldOnline()
        {
            Console.WriteLine("Let's go");

            ServerOptions.PrefixForwardedMessages = false;

            //CreateDummyWorld();
            CreateMidgaard();

            INetworkServer socketServer = new SocketServer(11000);
            Server.Server.Instance.Initialize(socketServer, false);
            Server.Server.Instance.Start();

            bool stopped = false;
            while (!stopped)
            {
                if (Console.KeyAvailable)
                {
                    string line = Console.ReadLine();
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        // server commands
                        if (line.StartsWith("#"))
                        {
                            line = line.Replace("#", String.Empty).ToLower();
                            if (line == "exit" || line == "quit")
                            {
                                stopped = true;
                                break;
                            }
                            else if (line == "alist")
                            {
                                Console.WriteLine("Admins:");
                                foreach (IAdmin a in Server.Server.Instance.GetAdmins())
                                    Console.WriteLine(a.Name + " " + a.PlayerState + " " + (a.Impersonating != null ? a.Impersonating.Name : "") + " " + (a.Incarnating != null ? a.Incarnating.Name : ""));
                            }
                            else if (line == "plist")
                            {
                                Console.WriteLine("players:");
                                foreach (IPlayer p in Server.Server.Instance.GetPlayers())
                                    Console.WriteLine(p.Name + " " + p.PlayerState + " " + (p.Impersonating != null ? p.Impersonating.Name : ""));
                            }
                            // TODO: characters/rooms/items
                        }
                    }
                }
                else
                    Thread.Sleep(100);
            }
            
            Server.Server.Instance.Stop();
        }

        private static void TestWorldOffline()
        {
            Console.WriteLine("Let's go");

            ServerOptions.PrefixForwardedMessages = false;
            //ServerOptions.PrefixForwardedMessages = true;
            //ServerOptions.ForwardSlaveMessages = true;

            //CreateDummyWorld();
            CreateMidgaard();

            ConsoleNetworkServer consoleNetworkServer = new ConsoleNetworkServer();
            Server.Server.Instance.Initialize(consoleNetworkServer, false);
            consoleNetworkServer.AddClient("Player1", false, true);
            Server.Server.Instance.Start(); // this call is blocking because consoleNetworkServer.Start is blocking

            Server.Server.Instance.Stop();
        }
    }
}
