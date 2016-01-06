﻿using Mud.Server.Constants;

namespace Mud.Server.Item
{
    public interface IItemArmor : IItem, IEquipable
    {
        WearLocations WearLocation { get; }
        int Armor { get; }
        ArmorKinds ArmorKind { get; }
        // TODO: modifier
    }
}