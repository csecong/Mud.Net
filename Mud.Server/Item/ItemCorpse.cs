﻿using System;
using System.Collections.Generic;
using Mud.Server.Blueprints;

namespace Mud.Server.Item
{
    public class ItemCorpse : ItemBase, IContainer
    {
        private readonly string _corpseName;
        private readonly List<IItem> _content;

        public override string DisplayName
        {
            get { return "The corpse of " + _corpseName; }
        }

        public ItemCorpse(Guid guid, ItemCorpseBlueprint blueprint, IContainer containedInto, ICharacter character)
            : base(guid, blueprint, containedInto)
        {
            _corpseName = character.DisplayName;
            Name = "corpse of " + _corpseName;
            // TODO: custom short description
            Description = "The corpse of " + _corpseName + " is laying here.";
            _content = new List<IItem>();
            foreach (IItem item in character.Content) // TODO: check stay death flag
                item.ChangeContainer(this);
        }

        public IReadOnlyCollection<IItem> Content { get { return _content; } }

        public bool Put(IItem obj)
        {
            //return false; // cannot put anything in a corpse, puttin something is done thru constructor
            _content.Add(obj);
            return true;
        }

        public bool Get(IItem obj)
        {
            return _content.Remove(obj);
        }
    }
}
