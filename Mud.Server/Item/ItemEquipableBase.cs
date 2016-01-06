﻿using System;
using Mud.Logger;
using Mud.Server.Blueprints;

namespace Mud.Server.Item
{
    public class ItemEquipableBase : ItemBase, IEquipable
    {
        public ItemEquipableBase(Guid guid, ItemBlueprintBase blueprint, IContainer containedInto) 
            : base(guid, blueprint, containedInto)
        {
        }

        public ICharacter EquipedBy { get; private set; }

        public bool ChangeEquipedBy(ICharacter character)
        {
            if (EquipedBy != null)
                EquipedBy.RemoveEquipment(this);
            Log.Default.WriteLine(LogLevels.Info, "ChangeEquipedBy: {0} : {1} -> {2}", Name, EquipedBy == null ? "<<??>>" : EquipedBy.Name, character == null ? "<<??>>" : character.Name);
            EquipedBy = character;
            return true;
        }
    }
}