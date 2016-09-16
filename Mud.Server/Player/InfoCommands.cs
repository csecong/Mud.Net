﻿using System.Text;
using Mud.Server.Common;
using Mud.Server.Helpers;
using Mud.Server.Input;

namespace Mud.Server.Player
{
    public partial class Player
    {
        [Command("who", Category = "Information")]
        protected virtual bool DoWho(string rawParameters, params CommandParameter[] parameters)
        {
            // TODO: title, additional informations
            StringBuilder sb = new StringBuilder();
            //
            sb.AppendFormatLine("Players:");
            foreach (IPlayer player in Repository.Server.Players)
            {
                switch (player.PlayerState)
                {
                    case PlayerStates.Impersonating:
                        if (player.Impersonating != null)
                            sb.AppendFormatLine("[ IG] {0} playing {1} [lvl: {2} Class: {3} Race: {4}]",
                                player.DisplayName,
                                player.Impersonating.DisplayName,
                                player.Impersonating.Level,
                                player.Impersonating.Class == null ? "(none)" : player.Impersonating.Class.DisplayName,
                                player.Impersonating.Race == null ? "(none)" : player.Impersonating.Race.DisplayName);
                        else
                            sb.AppendFormatLine("[ IG] {0} playing something", player.DisplayName);
                        break;
                    default:
                        sb.AppendFormatLine("[OOG] {0}", player.DisplayName);
                        break;
                }
            }
            //
            Page(sb);
            return true;
        }

        [Command("areas", Category = "Information", Priority = 10)]
        protected virtual bool DoAreas(string rawParameters, params CommandParameter[] parameters)
        {
            TableGenerator<IArea> generator = new TableGenerator<IArea>("Areas");
            generator.AddColumn("Name", 30, area => area.DisplayName, TableGenerator<IArea>.AlignLeftFunc);
            generator.AddColumn("Min", 5, area => area.MinLevel.ToString());
            generator.AddColumn("Max", 5, area => area.MaxLevel.ToString());
            generator.AddColumn("Builders", 15, area => area.Builders, TableGenerator<IArea>.AlignLeftFunc);
            generator.AddColumn("Credits", 40, area => area.Credits, TableGenerator<IArea>.AlignLeftFunc);
            StringBuilder sb = generator.Generate(Repository.World.Areas);
            Page(sb);
            return true;
        }
    }
}
