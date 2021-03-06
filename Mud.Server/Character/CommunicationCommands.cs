﻿using System.Linq;
using Mud.Container;
using Mud.Server.Helpers;
using Mud.Server.Input;

namespace Mud.Server.Character
{
    public partial class Character
    {
        [Command("say", Category = "Communication")]
        protected virtual bool DoSay(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0)
                Send("Say what?");
            else
            {
                //Send("%g%You say '%x%{0}%g%'%x%", rawParameters);
                //Act(ActOptions.ToRoom, "%g%{0:n} says '%x%{1}%g%'%x%", this, rawParameters);
                Act(ActOptions.ToAll, "%g%{0:N} say{0:v} '%x%{1}%g%'%x%", this, rawParameters);
            }
            return true;
        }

        [Command("yell", Category = "Communication")]
        protected virtual bool DoYell(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0)
                Send("Yell what?");
            else
            {
                //Send("%G%You yell %x%'{0}%G%'%x%", rawParameters);
                //foreach (IPlayer player in Room.Area.Players.Where(x => x.Impersonating != null))
                //    player.Impersonating.Act(ActOptions.ToCharacter, "%G%{0:n} yells '%x%{1}%G%'%x%", this, rawParameters);
                Act(Room.Area.Players.Where(x => x.Impersonating != null).Select(x => x.Impersonating), "%G%{0:n} yell{0:v} '%x%{1}%G%'%x%", this, rawParameters);
            }
            return true;
        }

        [Command("emote", Category = "Communication")]
        protected virtual bool DoEmote(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0)
            {
                Send("Emote what?");
                return true;
            }

            Act(ActOptions.ToAll, "{0:n} {1}", rawParameters);

            return true;
        }

        [Command("whisper", Category = "Communication")]
        protected virtual bool DoWhisper(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length <= 1)
            {
                Send("Whisper whom what?");
                return true;
            }

            ICharacter whom = FindHelpers.FindByName(Room.People, parameters[0]);
            if (whom == null)
            {
                Send(StringHelpers.CharacterNotFound);
                return true;
            }

            string what = CommandHelpers.JoinParameters(parameters.Skip(1));

            Act(ActOptions.ToCharacter, "You whisper '{0}' to {1:n}.", what, whom);
            whom.Act(ActOptions.ToCharacter, "{0:n} whispers you '{1}'.", this, what);
            ActToNotVictim(whom, "{0:n} whispers something to {1:n}.", this, whom);
            // ActOptions.ToAll cannot be used because 'something' is sent except for 'this' and 'whom'

            return true;
        }

        [Command("shout", Category = "Communication")]
        protected virtual bool DoShout(string rawParameters, params CommandParameter[] parameters)
        {
            if (parameters.Length == 0)
                Send("Shout what?");
            else
            {
                //Send($"You shout '{rawParameters}'");
                //foreach(IPlayer player in DependencyContainer.Instance.GetInstance<IServer>().Players.Where(x => x.Impersonating != null))
                //    player.Impersonating.Act(ActOptions.ToCharacter, "{0:n} shouts {1}", this, rawParameters);
                Act(DependencyContainer.Instance.GetInstance<IServer>().Players.Where(x => x.Impersonating != null).Select(x => x.Impersonating), "{0:N} shout{0:v} {1}", this, rawParameters);
            }
            return true;
        }
    }
}
