﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mud.Server.Constants;
using Mud.Server.Helpers;
using Mud.Server.Input;

namespace Mud.Server.Player
{
    public enum AvatarCreationStates
    {
        NameChoice, // -> NameConfirmation | NameChoice | Quit
        NameConfirmation, // -> NameChoice | SexChoice | Quit
        SexChoice, // -> SexChoice | RaceChoice | Quit
        RaceChoice, // -> RaceChoice | ClassChoice | Quit
        ClassChoice, // -> ClassChoice | AvatarCreated | ImmediateImpersonate | Quit
        ImmediateImpersonate, // -> CreationComplete
        CreationComplete,
        Quit
    }
    public class AvatarCreationStateMachine : InputTrapBase<IPlayer, AvatarCreationStates>
    {
        private string _name;
        private Sex _sex;
        private IRace _race;
        private IClass _class;

        public override bool IsFinalStateReached => State == AvatarCreationStates.CreationComplete || State == AvatarCreationStates.Quit;

        public AvatarCreationStateMachine()
        {
            PreserveInput = false;
            StateMachine = new Dictionary<AvatarCreationStates, Func<IPlayer, string, AvatarCreationStates>>
            {
                {AvatarCreationStates.NameChoice, ProcessNameChoice},
                {AvatarCreationStates.NameConfirmation, ProcessNameConfirmation},
                {AvatarCreationStates.SexChoice, ProcessSexChoice},
                {AvatarCreationStates.RaceChoice, ProcessRaceChoice},
                {AvatarCreationStates.ClassChoice, ProcessClassChoice},
                {AvatarCreationStates.ImmediateImpersonate, ProcessImmediateImpersonate},
                {AvatarCreationStates.CreationComplete, ProcessCreationComplete},
                {AvatarCreationStates.Quit, ProcessQuit}
            };
            State = AvatarCreationStates.NameChoice;
        }

        private AvatarCreationStates ProcessNameChoice(IPlayer player, string input)
        {
            // TODO: name validation
            if (!String.IsNullOrWhiteSpace(input))
            {
                if (input == "quit")
                {
                    player.Send("Creation cancelled"+Environment.NewLine);
                    return AvatarCreationStates.Quit;
                }
                _name = input;
                player.Send("Are you sure '{0}' is the name of your avatar? (y/n/quit)" + Environment.NewLine, StringHelpers.UpperFirstLetter(_name));
                return AvatarCreationStates.NameConfirmation;
            }
            player.Send("Please enter a name (type quit to stop creation):" + Environment.NewLine);
            return AvatarCreationStates.NameChoice;
        }

        private AvatarCreationStates ProcessNameConfirmation(IPlayer player, string input)
        {
            if (input == "y" || input == "yes")
            {
                player.Send("Nice! Please choose a sex (type quit to stop creation)." + Environment.NewLine);
                DisplaySexList(player);
                return AvatarCreationStates.SexChoice;
            }
            else if (input == "quit")
            {
                player.Send("Creation cancelled"+Environment.NewLine);
                return AvatarCreationStates.Quit;
            }
            player.Send("Ok, what name would you give to your avatar (type quit to stop creation)?");
            return AvatarCreationStates.NameChoice;
        }

        private AvatarCreationStates ProcessSexChoice(IPlayer player, string input)
        {
            if (input == "quit")
            {
                player.Send("Creation cancelled" + Environment.NewLine);
                return AvatarCreationStates.Quit;
            }
            bool found = EnumHelpers.TryFindByPrefix(input, out _sex);
            if (found)
            {
                player.Send("Great! Please choose a race (type quit to stop creation)." + Environment.NewLine);
                DisplayRaceList(player, false);
                return AvatarCreationStates.RaceChoice;
            }
            DisplaySexList(player);
            return AvatarCreationStates.SexChoice;
        }

        private AvatarCreationStates ProcessRaceChoice(IPlayer player, string input)
        {
            if (input == "quit")
            {
                player.Send("Creation cancelled" + Environment.NewLine);
                return AvatarCreationStates.Quit;
            }
            List<IRace> races = Repository.RaceManager.Races.Where(x => FindHelpers.StringStartWith(x.Name, input)).ToList();
            if (races.Count == 1)
            {
                _race = races[0];
                player.Send("Good choice! Now, please choose a class (type quit to stop creation)." + Environment.NewLine);
                DisplayClassList(player, false);
                return AvatarCreationStates.ClassChoice;
            }
            DisplayRaceList(player);
            return AvatarCreationStates.RaceChoice;
        }

        private AvatarCreationStates ProcessClassChoice(IPlayer player, string input)
        {
            if (input == "quit")
            {
                player.Send("Creation cancelled" + Environment.NewLine);
                return AvatarCreationStates.Quit;
            }
            List<IClass> classes = Repository.ClassManager.Classes.Where(x => FindHelpers.StringStartWith(x.Name, input)).ToList();
            if (classes.Count == 1)
            {
                _class = classes[0];
                // TODO: Add character to impersonate list
                player.Save();
                // TODO: better wording
                player.Send("Your avatar is created. Name: {0} Sex: {1} Race: {2} Class: {3}."+Environment.NewLine, StringHelpers.UpperFirstLetter(_name), _sex, _race.DisplayName, _class.DisplayName);
                player.Send("Would you like to impersonate it now? (y/n)"+Environment.NewLine);
                return AvatarCreationStates.ImmediateImpersonate;
            }
            DisplayClassList(player);
            return AvatarCreationStates.ClassChoice;
        }

        private AvatarCreationStates ProcessImmediateImpersonate(IPlayer player, string input)
        {
            if (input == "y" || input == "yes")
            {
                // Create character + add in world
                IRoom startingRoom = Repository.World.Rooms.FirstOrDefault(x => x.Name.ToLower() == "the temple of mota"); // todo: mud school
                ICharacter avatar = Repository.World.AddCharacter(Guid.NewGuid(), _name, _class, _race, _sex, startingRoom);
                // TODO: impersonate character with an internal command
                State = AvatarCreationStates.CreationComplete;
                player.ProcessCommand("/impersonate " + avatar.Name);
                return AvatarCreationStates.CreationComplete;
            }
            player.Send("Avatar {0} created but not impersonated. Use /impersonate {0} to enter game or use /list to see your avatar list."+Environment.NewLine, StringHelpers.UpperFirstLetter(_name));
            // else, NOP
            return AvatarCreationStates.CreationComplete;
        }

        private AvatarCreationStates ProcessCreationComplete(IPlayer player, string input)
        {
            // fall-thru
            return AvatarCreationStates.CreationComplete;
        }

        private AvatarCreationStates ProcessQuit(IPlayer player, string input)
        {
            // fall-thru
            return AvatarCreationStates.Quit;
        }

        private void DisplaySexList(IPlayer player, bool displayHeader = true)
        {
            if (displayHeader)
                player.Send("Please choose a sex (type quit to stop creation)." + Environment.NewLine);
            string sex = String.Join(" | ", Enum.GetNames(typeof(Sex)));
            player.Send(sex+Environment.NewLine);
        }

        private void DisplayRaceList(IPlayer player, bool displayHeader = true)
        {
            if (displayHeader)
                player.Send("Please choose a race (type quit to stop creation)." + Environment.NewLine);
            string races = String.Join(" | ", Repository.RaceManager.Races.Select(x => x.DisplayName));
            player.Send(races + Environment.NewLine);
        }

        private void DisplayClassList(IPlayer player, bool displayHeader = true)
        {
            if (displayHeader)
                player.Send("Please choose a class (type quit to stop creation)." + Environment.NewLine);
            string classes = String.Join(" | ", Repository.ClassManager.Classes.Select(x => x.DisplayName));
            player.Send(classes + Environment.NewLine);
        }
    }
}
