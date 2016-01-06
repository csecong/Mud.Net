﻿using System;
using Mud.Server.Helpers;
using Mud.Server.Input;

namespace Mud.Server.Admin
{
    public partial class Admin
    {
        [Command("incarnate")]
        protected virtual bool DoIncarnate(string rawParameters, params CommandParameter[] parameters)
        {
            if (Impersonating != null)
                Send("You are already impersonating {0}." + Environment.NewLine, Impersonating.DisplayName);
            else if (parameters.Length == 0)
            {
                if (Incarnating != null)
                {
                    Send("%M%You stop incarnating %C%{0}%x%." + Environment.NewLine, Incarnating.DisplayName);
                    Incarnating.ChangeIncarnation(null);
                }
                else
                    Send("Syntax: Incarnate <kind> <name|id>" + Environment.NewLine);
            }
            else if (parameters.Length == 1)
                Send("Syntax: Incarnate <kind> <name|id>" + Environment.NewLine);
            else if (parameters.Length == 2)
            {
                IEntity incarnateTarget = null;
                string kind = parameters[0].Value;
                if ("room".StartsWith(kind))
                    incarnateTarget = FindHelpers.FindByName(World.World.Instance.GetRooms(), parameters[1]);
                else if ("item".StartsWith(kind))
                    incarnateTarget = FindHelpers.FindByName(World.World.Instance.GetItems(), parameters[1]);
                else if ("mob".StartsWith(kind))
                    incarnateTarget = FindHelpers.FindByName(World.World.Instance.GetCharacters(), parameters[1]);
                if (incarnateTarget == null)
                    Send("Target not found");
                else
                {
                    if (Incarnating != null)
                    {
                        Send("%M%You stop incarnating %C%{0}%x%." + Environment.NewLine, Incarnating.DisplayName);
                        Incarnating.ChangeIncarnation(null);
                    }
                    Send("%M%You start incarnating %C%{0}%x%." + Environment.NewLine, incarnateTarget.DisplayName);
                    incarnateTarget.ChangeIncarnation(this);
                    Incarnating = incarnateTarget;
                    PlayerState = PlayerStates.Impersonating;
                }
            }
            return true;
        }
    }
}