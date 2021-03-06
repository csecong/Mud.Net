﻿using System.Collections.Generic;
using Mud.Server.Constants;

namespace Mud.Server.Classes
{
    public class Priest : ClassBase
    {
        #region IClass

        public override string Name => "priest";

        public override string ShortName => "Pri";

        public override IEnumerable<ResourceKinds> ResourceKinds { get; } = new List<ResourceKinds>
        {
            Constants.ResourceKinds.Mana
        };

        public override IEnumerable<ResourceKinds> CurrentResourceKinds(Forms form)
        {
            return ResourceKinds; // always mana
        }

        public override int GetPrimaryAttributeByLevel(PrimaryAttributeTypes primaryAttribute, int level)
        {
            return level * 10; // TODO: http://wow.gamepedia.com/Base_attributes
        }

        #endregion

        public Priest()
        {
            AddAbility(1, "renew");
            AddAbility(1, "power word: shield");
            AddAbility(1, "shadow Word: pain");
            AddAbility(20, "shadow form");
        }
    }
}
