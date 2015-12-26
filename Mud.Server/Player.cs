﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Mud.Logger;

namespace Mud.Server
{
    public class Player : IPlayer
    {
        private static readonly IReadOnlyDictionary<string, MethodInfo> Commands;

        static Player()
        {
            Commands = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.GetCustomAttributes(typeof(CommandAttribute), false).Any())
                .ToDictionary(x => x.GetCustomAttributes(typeof(CommandAttribute)).OfType<CommandAttribute>().First().Name);
        }

        public bool ProcessCommand(string commandLine)
        {
            string command;
            string rawParameters;
            CommandParameter[] parameters;
            bool forceOutOfGame;

            // Extract command and parameters
            bool extractedSuccessfully = CommandHelpers.ExtractCommandAndParameters(commandLine, out command, out rawParameters, out parameters, out forceOutOfGame);
            if (!extractedSuccessfully)
            {
                Log.Default.WriteLine(LogLevels.Warning, "Command and parameters not extracted successfully");
                return false;
            }

            bool executedSuccessfully;
            if (forceOutOfGame || Impersonating == null)
            {
                Log.Default.WriteLine(LogLevels.Info,  "Executing [{0}] as IPlayer command", command);
                executedSuccessfully = ExecuteCommand(command, rawParameters, parameters);
            }
            else
            {
                Log.Default.WriteLine(LogLevels.Info, "Executing [{0}] as impersonated(ICharacter) command", command);
                executedSuccessfully = Impersonating.ExecuteCommand(command, rawParameters, parameters);
            }

            return executedSuccessfully;
        }

        public bool ExecuteCommand(string command, string rawParameters, CommandParameter[] parameters)
        {
            // Search for command
            MethodInfo methodInfo;
            if (Commands.TryGetValue(command, out methodInfo))
            {
                bool executedSuccessfully = (bool)methodInfo.Invoke(this, new object[] { rawParameters, parameters });
                if (!executedSuccessfully)
                {
                    Log.Default.WriteLine(LogLevels.Warning, "Error while executing command");
                    return false;
                }

                return true;
            }
            return false;
        }

        public void Send(string message)
        {
            throw new NotImplementedException();
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public ICharacter Impersonating { get; private set; }

        public DateTime LastCommandTimestamp { get; private set; }
        public string LastCommand { get; private set; }

        public bool GoInGame(ICharacter character)
        {
            // TODO: check validity
            Impersonating = character;
            return true;
        }

        public bool GoOutOfGame()
        {
            // TODO
            Impersonating = null;
            return true;
        }

        public void OnDisconnected()
        {
        }

        [Command("tell")]
        protected virtual bool Tell(string rawParameters, CommandParameter[] parameters)
        {
            return true;
        }

        [Command("impersonate")]
        protected virtual bool Impersonate(string rawParameters, CommandParameter[] parameters)
        {
            return true;
        }

        [Command("test")]
        protected virtual bool Test(string rawParameters, CommandParameter[] parameters)
        {
            return true;
        }
    }
}
