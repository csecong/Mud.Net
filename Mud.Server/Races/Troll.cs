﻿namespace Mud.Server.Races
{
    public class Troll : RaceBase
    {
        public override string Name => "Troll";

        public override string ShortName => "Tro";

        public Troll()
        {
            AddAbility(1, "berserking");
        }
    }
}
