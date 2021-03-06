﻿using System;
using System.Linq;
using System.Text;
using Mud.Container;
using Mud.Server.Blueprints.Character;
using Mud.Server.Blueprints.Item;
using Mud.Server.Constants;
using Mud.Server.Helpers;
using Mud.Server.Input;
using Mud.Server.Server;

namespace Mud.Server.Admin
{
    public partial class Admin
    {
        [AdminCommand("shutdown", Category = "Admin", Priority = 999 /*low priority*/, NoShortcut = true, MinLevel = AdminLevels.Implementor, CannotBeImpersonated = true)]
        protected virtual bool DoShutdown(string rawParameters, params CommandParameter[] parameters)
        {
            int seconds;
            if (parameters.Length == 0 || !int.TryParse(parameters[0].Value, out seconds))
                Send("Syntax: shutdown <delay>");
            else if (seconds < 30)
                Send("You cannot shutdown that fast.");
            else
                DependencyContainer.Instance.GetInstance<IServer>().Shutdown(seconds);
            return true;
        }

        [AdminCommand("cload", Category = "Admin", MustBeImpersonated = true)]
        [AdminCommand("mload", Category = "Admin", MustBeImpersonated = true)]
        protected virtual bool DoCload(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0 || !parameters[0].IsNumber)
            {
                Send("Syntax: cload <id>");
                return true;
            }

            CharacterBlueprint characterBlueprint = DependencyContainer.Instance.GetInstance<IWorld>().GetCharacterBlueprint(parameters[0].AsNumber);
            if (characterBlueprint == null)
            {
                Send("No character with that id.");
                return true;
            }

            ICharacter character = DependencyContainer.Instance.GetInstance<IWorld>().AddCharacter(Guid.NewGuid(), characterBlueprint, Impersonating.Room);
            if (character == null)
            {
                Send("Character cannot be created.");
                DependencyContainer.Instance.GetInstance<IServer>().Wiznet($"DoCload: character with id {parameters[0].AsNumber} cannot be created", WiznetFlags.Bugs, AdminLevels.Implementor);
                return true;
            }

            DependencyContainer.Instance.GetInstance<IServer>().Wiznet($"{DisplayName} loads {character.DebugName}.", WiznetFlags.Load);

            Impersonating.Act(ActOptions.ToAll, "{0:N} {0:h} created {1:n}!", Impersonating, character);
            Send("Ok.");

            return true;
        }

        [AdminCommand("iload", Category = "Admin", MustBeImpersonated = true)]
        [AdminCommand("oload", Category = "Admin", MustBeImpersonated = true)]
        protected virtual bool DoIload(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0 || !parameters[0].IsNumber)
            {
                Send("Syntax: iload <id>");
                return true;
            }

            ItemBlueprintBase itemBlueprint = DependencyContainer.Instance.GetInstance<IWorld>().GetItemBlueprint(parameters[0].AsNumber);
            if (itemBlueprint == null)
            {
                Send("No item with that id.");
                return true;
            }

            IContainer container = itemBlueprint.WearLocation == WearLocations.None ? Impersonating.Room as IContainer : Impersonating as IContainer;
            IItem item = DependencyContainer.Instance.GetInstance<IWorld>().AddItem(Guid.NewGuid(), itemBlueprint, container);
            if (item == null)
            {
                Send("Item cannot be created.");
                DependencyContainer.Instance.GetInstance<IServer>().Wiznet($"DoIload: item with id {parameters[0].AsNumber} cannot be created", WiznetFlags.Bugs, AdminLevels.Implementor);
                return true;
            }

            DependencyContainer.Instance.GetInstance<IServer>().Wiznet($"{DisplayName} loads {item.DebugName}.", WiznetFlags.Load);

            Impersonating.Act(ActOptions.ToAll, "{0:N} {0:h} created {1}!", Impersonating, item);
            Send("Ok.");

            return true;
        }

        [Command("slay", Category = "Admin", NoShortcut = true)]
        protected virtual bool DoSlay(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0)
            {
                Send("Slay whom?");
                return true;
            }

            ICharacter victim = FindHelpers.FindByName(Impersonating.Room.People, parameters[0]);
            if (victim == null)
            {
                Send(StringHelpers.CharacterNotFound);
                return true;
            }

            if (victim == Impersonating)
            {
                Send("Suicide is a mortal sin.");
                return true;
            }

            DependencyContainer.Instance.GetInstance<IServer>().Wiznet($"{DisplayName} slayed {victim.DebugName}.", WiznetFlags.Punish);

            victim.Act(ActOptions.ToAll, "{0:N} slay{0:v} {1} in cold blood!", Impersonating, victim);
            victim.RawKilled(Impersonating, false);

            return true;
        }

        [AdminCommand("purge", Category = "Admin", NoShortcut = true, MustBeImpersonated = true)]
        protected virtual bool DoPurge(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0)
            {
                Send("Purge what?");
                return true;
            }

            IItem item = FindHelpers.FindItemHere(Impersonating, parameters[0]);
            if (item == null)
            {
                Send(StringHelpers.ItemNotFound);
                return true;
            }

            DependencyContainer.Instance.GetInstance<IServer>().Wiznet($"{DisplayName} purges {item.DebugName}.", WiznetFlags.Punish);

            Impersonating.Act(ActOptions.ToAll, "{0:N} purge{0:v} {1}!", Impersonating, item);
            DependencyContainer.Instance.GetInstance<IWorld>().RemoveItem(item);

            return true;
        }

        [AdminCommand("goto", Category = "Admin", MustBeImpersonated = true)]
        protected virtual bool DoGoto(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0)
            {
                Send("Goto where?");
                return true;
            }

            //
            IRoom where = FindHelpers.FindLocation(Impersonating, parameters[0]);
            if (where == null)
            {
                Send("No such location.");
                return true;
            }

            if (Impersonating.Fighting != null)
                Impersonating.StopFighting(true);
            Impersonating.Act(Impersonating.Room.People.Where(x => x != Impersonating && x.CanSee(Impersonating)), "{0} leaves in a swirling mist.", Impersonating); // Don't display 'Someone leaves ...' if Impersonating is not visible
            Impersonating.ChangeRoom(where);
            Impersonating.Act(Impersonating.Room.People.Where(x => x != Impersonating && x.CanSee(Impersonating)), "{0} appears in a swirling mist.", Impersonating);
            Impersonating.AutoLook();

            return true;
        }

        [Command("xpbonus", Category = "Admin")]
        protected virtual bool DpXpBonus(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length < 2)
            {
                Send("Syntax: xpgain <character> <experience>");
                return true;
            }

            ICharacter victim = FindHelpers.FindByName(DependencyContainer.Instance.GetInstance<IServer>().Players.Where(x => x.Impersonating != null).Select(x => x.Impersonating), parameters[0]);
            if (victim == null)
            {
                Send("That impersonated player is not here.");
                return true;
            }

            int experience = parameters[1].AsNumber;
            if (experience < 1)
            {
                Send("Experience must be greater than 1.");
                return true;
            }

            if (victim.Level >= ServerOptions.MaxLevel)
            {
                Send($"{DisplayName} is already at max level.");
                return true;
            }

            DependencyContainer.Instance.GetInstance<IServer>().Wiznet($"{DisplayName} give experience [{experience}] to {victim.DebugName}.", WiznetFlags.Help);

            victim.Send("You have received an experience boost.");
            victim.GainExperience(experience);

            //
            victim.ImpersonatedBy.Save();
            return true;
        }

        [Command("transfer", Category = "Admin")]
        protected virtual bool DoTransfer(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0)
            {
                Send("Transfer whom (and where)?");
                return true;
            }
            if (Impersonating == null && parameters.Length == 1)
            {
                Send("Transfer without specifying location can only be used when impersonating.");
                return true;
            }

            // TODO: IsAll ?

            IRoom where;
            if (Impersonating != null)
            {
                where = parameters.Length == 1
                    ? Impersonating.Room
                    : FindHelpers.FindLocation(Impersonating, parameters[1]);
            }
            else
                where = FindHelpers.FindLocation(parameters[1]);
            if (where == null)
            {
                Send("No such location.");
                return true;
            }

            ICharacter whom;
            if (Impersonating != null)
                whom = FindHelpers.FindChararacterInWorld(Impersonating, parameters[0]);
            else
                whom = FindHelpers.FindByName(DependencyContainer.Instance.GetInstance<IWorld>().Characters, parameters[0]);
            if (whom == null)
            {
                Send(StringHelpers.CharacterNotFound);
                return true;
            }

            if (whom.Fighting != null)
                whom.StopFighting(true);
            whom.Act(ActOptions.ToRoom, "{0:N} disappears in a mushroom cloud.", whom);
            whom.ChangeRoom(where);
            whom.Act(ActOptions.ToRoom, "{0:N} appears from a puff of smoke.", whom);
            if (whom != Impersonating)
            {
                if (Impersonating != null)
                    whom.Act(ActOptions.ToCharacter, "{0:N} has transferred you.", Impersonating);
                else
                    whom.Act(ActOptions.ToCharacter, "Someone has transferred you.");
            }
            whom.AutoLook();

            Send("Ok");
            return true;
        }

        [Command("sanitycheck", Category = "Admin")]
        protected virtual bool DoSanityCheck(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0)
            {
                Send("Perform sanity check on which player/admin?");
                return true;
            }

            IPlayer whom = FindHelpers.FindByName(DependencyContainer.Instance.GetInstance<IServer>().Players, parameters[0]);

            if (whom == null)
            {
                Send(StringHelpers.CharacterNotFound);
                return true;
            }

            StringBuilder info = whom.PerformSanityCheck();
            Page(info);

            return true;
        }
    }
}
