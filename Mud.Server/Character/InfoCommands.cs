﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mud.Logger;
using Mud.Server.Constants;
using Mud.Server.Helpers;
using Mud.Server.Input;
using Mud.Server.Server;

namespace Mud.Server.Character
{
    public partial class Character
    {
        // 0/ if sleeping/blind/room is dark
        // 1/ else if no parameter, look in room
        // 2/ else if 1st parameter is 'in' or 'on', search item (matching 2nd parameter) in the room, then inventory, then equipment, and display its content
        // 3/ else if a character can be found in room (matching 1st parameter), display character info
        // 4/ else if an item can be found in inventory+room (matching 1st parameter), display item description or extra description
        // 5/ else, if an extra description can be found in room (matching 1st parameter), display it
        // 6/ else, if 1st parameter is a direction, display if there is an exit/door
        [Command("look")]
        protected virtual bool DoLook(string rawParameters, params CommandParameter[] parameters)
        {
            // TODO: 0/ sleeping/blind/dark room (see act_info.C:1413 -> 1436)

            // 1: room+exits+chars+items
            if (String.IsNullOrWhiteSpace(rawParameters))
            {
                Log.Default.WriteLine(LogLevels.Debug, "DoLook(1): room");
                DisplayRoom();
                return true;
            }
            // 2: container in room then inventory then equipment
            if (parameters[0].Value == "in" || parameters[0].Value == "on" || parameters[0].Value == "into")
            {
                Log.Default.WriteLine(LogLevels.Debug, "DoLook(2): container in room, inventory, equipment");
                // look in container
                if (parameters.Length == 1)
                    Send("Look in what?" + Environment.NewLine);
                else
                {
                    // search in room, then in inventory(unequiped), then in equipement
                    IItem containerItem = FindHelpers.FindCharacterItemByName(this, parameters[1]);
                    if (containerItem == null)
                        Send(StringHelpers.ItemNotFound);
                    else
                    {
                        Log.Default.WriteLine(LogLevels.Debug, "DoLook(2): found in {0}", containerItem.ContainedInto.Name);
                        IContainer container = containerItem as IContainer;
                        if (container != null)
                        {
                            // TODO: check if closed
                            Send("{0} holds:" + Environment.NewLine, containerItem.DisplayName);
                            DisplayItems(container.Content, true, true);
                        }
                        // TODO: drink container
                        else
                            Send("This is not a container." + Environment.NewLine);
                    }
                }
                return true;
            }
            // 3: character in room
            ICharacter victim = FindHelpers.FindByName(Room.People.Where(CanSee), parameters[0]);
            if (victim != null)
            {
                Log.Default.WriteLine(LogLevels.Debug, "DoLook(3): character in room");
                // TODO: peek ability check ???
                DisplayCharacter(victim, true);
                return true;
            }
            // 4: search n'th item in inventory+room
            //IItem item = FindHelpers.FindByName(Content.Concat(Room.Content), parameters[0]); // Concat preserves order!!!
            IItem item = FindHelpers.FindCharacterItemByName(this, parameters[0]);
            if (item != null)
            {
                Log.Default.WriteLine(LogLevels.Debug, "DoLook(4+5): item in inventory+room -> {0}", item.ContainedInto.Name);
                Send("{0}" + Environment.NewLine, item.Description); // TODO: formatting
                return true;
            }
            // 6: extra description in room  TODO
            // 7: direction
            ServerOptions.ExitDirections direction;
            if (EnumHelpers.TryFindByName(parameters[0].Value, out direction))
            {
                Log.Default.WriteLine(LogLevels.Debug, "DoLook(7): direction");
                IExit exit = Room.Exit(direction);
                if (exit == null || exit.Destination == null)
                    Send("Nothing special there." + Environment.NewLine);
                else
                {
                    if (exit.Description != null)
                        Send(exit.Description);
                    else
                        Send("Nothing special there." + Environment.NewLine);
                    // TODO: check if door + flags CLOSED/BASHED/HIDDEN
                }
            }
            else
                Send(StringHelpers.ItemNotFound);
            return true;
        }

        [Command("exits")]
        protected virtual bool DoExits(string rawParameters, params CommandParameter[] parameters)
        {
            DisplayExits(false);
            return true;
        }

        [Command("inventory")]
        protected virtual bool DoInventory(string rawParameters, params CommandParameter[] parameters)
        {
            Send("You are carrying:" + Environment.NewLine);
            DisplayItems(Content, true, true);
            return true;
        }

        [Command("equipment")]
        protected virtual bool DoEquipment(string rawParameters, params CommandParameter[] parameters)
        {
            Send("You are using:" + Environment.NewLine);
            if (Equipments.All(x => x.Item == null))
                Send("Nothing" + Environment.NewLine);
            else
                foreach (EquipedItem equipedItem in Equipments.Where(x => x.Item != null))
                {
                    string where = EquipmentSlotsToString(equipedItem.Slot);
                    StringBuilder sb = new StringBuilder(where);
                    if (CanSee(equipedItem.Item))
                        sb.AppendLine(FormatItem(equipedItem.Item, true));
                    else
                        sb.AppendLine("something.");
                    Send(sb);
                }
            return true;
        }

        [Command("examine")]
        protected virtual bool DoExamine(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0)
                Send("Examine what or whom?" + Environment.NewLine);
            else
            {
                ICharacter victim = FindHelpers.FindByName(Room.People, parameters[0]);
                if (victim != null)
                {
                    Act(ActOptions.ToCharacter, "You examine {0}.", victim);
                    victim.Act(ActOptions.ToCharacter, "{0} examines you.", this);
                    ActToNotVictim(victim, "{0} examines {1}.", this, victim);
                    //DoLook(rawParameters, parameters); // call immediately helpers function (DoLook: case 3)
                    DisplayCharacter(victim, true);
                    // TODO: display race and size
                }
                else
                {
                    IItem item = FindHelpers.FindCharacterItemByName(this, parameters[0]);
                    if (item != null)
                    {
                        Act(ActOptions.ToCharacter, "You examine {0}.", item);
                        Act(ActOptions.ToRoom, "{0} examines {1}.", this, item);
                        DoLook(rawParameters, parameters); // TODO: call immediately sub-function
                        IContainer container = item as IContainer;
                        if (container != null) // if container, display content
                        {
                            List<CommandParameter> newParameters = new List<CommandParameter>(parameters);
                            newParameters.Insert(0, new CommandParameter
                            {
                                Count = 1,
                                Value = "in"
                            });
                            DoLook("in " + rawParameters, newParameters.ToArray()); // TODO: call immediately sub-function
                        }
                    }
                    else
                        Send("You don't see any {0}." + Environment.NewLine, parameters[0]);
                }
            }
            return true;
        }

        [Command("scan")]
        protected virtual bool DoScan(string rawParameters, params CommandParameter[] parameters)
        {
            // Current room
            Send("Right here you see:" + Environment.NewLine);
            StringBuilder currentScan = ScanRoom(Room);
            if (currentScan.Length == 0)
                Send("None" + Environment.NewLine); // should never happen, 'this' is in the room
            else
                Send(currentScan); // no need to add CRLF
            // Scan in one direction for each distance, then starts with another direction
            foreach (ServerOptions.ExitDirections direction in EnumHelpers.GetValues<ServerOptions.ExitDirections>())
            {
                IRoom currentRoom = Room; // starting point
                for (int distance = 1; distance < 4; distance++)
                {
                    IRoom destination = currentRoom.GetRoom(direction);
                    if (destination == null)
                        break; // stop in that direction if no exit found
                    StringBuilder roomScan = ScanRoom(destination);
                    if (roomScan.Length > 0)
                    {
                        Send("%c%{0} %r%{1}%x% from here you see:" + Environment.NewLine, distance, direction);
                        Send(roomScan); // no need to add CRLF
                        currentRoom = destination;
                    }
                }
            }
            return true;
        }

        [Command("affects")]
        protected virtual bool DoAffects(string rawParameters, params CommandParameter[] parameters)
        {
            // TODO: better UI
            StringBuilder sb = new StringBuilder();
            // Auras
            if (_auras.Any())
            {
                sb.AppendLine("Auras:");
                foreach (IAura aura in _auras)
                    if (aura.Modifier == AuraModifiers.None)
                        sb.AppendFormatLine("{0} for {1}.",
                        aura.Name,
                        StringHelpers.FormatDelay(aura.SecondsLeft));
                    else
                        sb.AppendFormatLine("{0} modifies {1} by {2}{3} for {4}.", 
                            aura.Name, 
                            aura.Modifier, 
                            aura.Amount, 
                            aura.AmountOperator == AmountOperators.Fixed ? String.Empty : "%",
                            StringHelpers.FormatDelay(aura.SecondsLeft));
            }
            else
                sb.AppendLine("No aura.");
            // Periodic auras
            if (_periodicAuras.Any())
            {
                sb.AppendLine("Periodic auras:");
                foreach (IPeriodicAura pa in _periodicAuras)
                {
                    if (pa.AuraType == PeriodicAuraTypes.Damage) // TODO: operator
                        sb.AppendFormatLine("{0} from {1}: {2} {3}{4} damage every {5} for {6}.",
                            pa.Name,
                            pa.Source == null ? "(none)" : pa.Source.DisplayName,
                            pa.Amount,
                            pa.AmountOperator == AmountOperators.Fixed ? String.Empty : "%",
                            pa.School,
                            StringHelpers.FormatDelay(pa.TickDelay),
                            StringHelpers.FormatDelay(pa.SecondsLeft));
                    else
                        sb.AppendFormatLine("{0} from {1}: {2}{3} heal every {4} for {5}.",
                            pa.Name,
                            pa.Source == null ? "(none)" : pa.Source.DisplayName,
                            pa.Amount,
                            pa.AmountOperator == AmountOperators.Fixed ? String.Empty : "%",
                            StringHelpers.FormatDelay(pa.TickDelay),
                            StringHelpers.FormatDelay(pa.SecondsLeft));
                }
            }
            else
                sb.AppendLine("No periodic aura.");
            Send(sb);
            return true;
        }

        //********************************************************************
        // Helpers
        //********************************************************************
        private void DisplayRoom() // equivalent to act_info.C:do_look("auto")
        {
            // Room name
            Send("%c%{0}%x%" + Environment.NewLine, Room.DisplayName);
            // Room description
            Send(Room.Description);
            // Exits
            DisplayExits(true);
            DisplayItems(Room.Content, false, false);
            foreach (ICharacter victim in Room.People.Where(x => x != this))
            { //  (see act_info.C:714 show_char_to_char)
                if (CanSee(victim)) // see act_info.C:375 show_char_to_char_0)
                {
                    // TODO: display flags (see act_info.C:387 -> 478)
                    // TODO: display long description and stop
                    // TODO: display position (see act_info.C:505 -> 612)
                    Send("{0} is here." + Environment.NewLine, victim.DisplayName); // last case of POS_STANDING
                }
                else
                    ; // TODO: INFRARED (see act_info.C:728)
            }
        }

        private void DisplayCharacter(ICharacter victim, bool peekInventory) // equivalent to act_info.C:show_char_to_char_1
        {
            if (this == victim)
                Act(ActOptions.ToRoom, "{0} looks at {0:m}self.", this);
            else
            {
                victim.Act(ActOptions.ToCharacter, "{0} looks at you.", this);
                ActToNotVictim(victim, "{0} looks at {1}.", this, victim);
            }
            Send("{0} is here." + Environment.NewLine, victim.DisplayName);
            // TODO: health (instead of is here.) (see act_info.C:629 show_char_to_char_1)
            if (victim.Equipments.Any(x => x.Item != null))
            {
                Act(ActOptions.ToCharacter, "{0} is using:", victim);
                foreach (EquipedItem equipedItem in victim.Equipments.Where(x => x.Item != null && CanSee(x.Item)))
                {
                    string where = EquipmentSlotsToString(equipedItem.Slot);
                    StringBuilder sb = new StringBuilder(where);
                    sb.AppendLine(FormatItem(equipedItem.Item, true));
                    Send(sb);
                }
            }
            

            if (peekInventory)
            {
                Send("You peek at the inventory:" + Environment.NewLine);
                DisplayItems(victim.Content, true, true);
            }
        }

        private void DisplayItems(IReadOnlyCollection<IItem> items, bool shortDisplay, bool displayNothing) // equivalent to act_info.C:show_list_to_char
        {
            if (displayNothing && !items.Any())
                Send("Nothing." + Environment.NewLine);
            else
            {
                foreach (IItem item in items) // TODO: compact mode (grouped by Blueprint)
                    Send(FormatItem(item, shortDisplay) + Environment.NewLine); // TODO: (see act_info.C:170 format_obj_to_char)
            }
        }

        private void DisplayExits(bool compact)
        {
            StringBuilder message = new StringBuilder();
            if (compact)
                message.Append("[Exits:");
            else
                message.AppendLine("Obvious exits:");
            bool exitFound = false;
            foreach (ServerOptions.ExitDirections direction in EnumHelpers.GetValues<ServerOptions.ExitDirections>())
            {
                IExit exit = Room.Exit(direction);
                if (exit != null && exit.Destination != null) // TODO: test if destination room is visible, if exit is visible, ...
                {
                    if (compact)
                    {
                        // TODO: 
                        // hidden+not bashed: [xxx]
                        // closed: (xxx)
                        // bashed: {{xxx}}
                        message.AppendFormat(" {0}", direction.ToString().ToLowerInvariant());
                    }
                    else
                    {
                        string destination = exit.Destination.DisplayName; // TODO: 'room name' or 'too dark to tell' or 'closed door'
                        message.AppendFormatLine("{0} - {1} {2}{3}{4}",
                            StringHelpers.UpperFirstLetter(direction.ToString()),
                            destination,
                            String.Empty, // TODO: closed (DOOR)
                            String.Empty, // TODO: hidden [HIDDEN]
                            String.Empty); // TODO: {{BASHED}}
                    }
                    exitFound = true;
                }
            }
            if (!exitFound)
            {
                if (compact)
                    message.AppendLine(" none");
                else
                    message.AppendLine("None.");
            }
            if (compact)
                message.AppendLine("]");
            Send(message);
        }

        private StringBuilder ScanRoom(IRoom room)
        {
            StringBuilder peopleInRoom = new StringBuilder();
            foreach (ICharacter victim in room.People.Where(CanSee))
                peopleInRoom.AppendFormatLine(" - {0}", victim.DisplayName);
            return peopleInRoom;
        }

        private static string EquipmentSlotsToString(EquipmentSlots slot)
        {
            switch (slot)
            {
                case EquipmentSlots.Light:
                    return "%C%<used as light>         %x%";
                case EquipmentSlots.Head:
                    return "%C%<worn on head>          %x%";
                case EquipmentSlots.Amulet:
                    return "%C%<worn on neck>          %x%";
                case EquipmentSlots.Shoulders:
                    return "%C%<worn around shoulders> %x%";
                case EquipmentSlots.Chest:
                    return "%C%<worn on chest>         %x%";
                case EquipmentSlots.Cloak:
                    return "%C%<worn about body>       %x%";
                case EquipmentSlots.Waist:
                    return "%C%<worn about waist>      %x%";
                case EquipmentSlots.Wrists:
                    return "%C%<worn around wrists>    %x%";
                case EquipmentSlots.Hands:
                    return "%C%<worn on hands>         %x%";
                case EquipmentSlots.RingLeft:
                    return "%C%<worn on left finger>   %x%";
                case EquipmentSlots.RingRight:
                    return "%C%<worn on right finger>   %x%";
                case EquipmentSlots.Legs:
                    return "%C%<worn on legs>          %x%";
                case EquipmentSlots.Feet:
                    return "%C%<worn on feet>          %x%";
                case EquipmentSlots.Trinket1:
                    return "%C%<worn as 1st trinket>   %x%";
                case EquipmentSlots.Trinket2:
                    return "%C%<worn as 2nd trinket>   %x%";
                case EquipmentSlots.Wield:
                    return "%C%<wielded>               %x%";
                case EquipmentSlots.Wield2:
                    return "%c%<offhand>               %x%";
                case EquipmentSlots.Hold:
                    return "%C%<held>                  %x%";
                case EquipmentSlots.Shield:
                    return "%C%<worn as shield>        %x%";
                case EquipmentSlots.Wield2H:
                    return "%C%<wielded 2-handed>      %x%";
                default:
                    Log.Default.WriteLine(LogLevels.Error, "DoEquipment: missing WearLocation {0}", slot);
                    break;
            }
            return "%C%<unknown>               %x%";
        }

        private static string FormatItem(IItem item, bool shortDisplay) // TODO: (see act_info.C:170 format_obj_to_char)
        {
            StringBuilder sb = new StringBuilder();
            // TODO: affects
            sb.Append(shortDisplay
                ? item.DisplayName
                : item.Description);
            return sb.ToString();
        }
    }
}
