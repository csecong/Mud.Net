﻿using System;
using System.Collections.Generic;
using Mud.Server.Blueprints;
using Mud.Server.Constants;
using Mud.Server.Helpers;
using Mud.Server.Input;
using Mud.Server.Item;
using Mud.Server.Server;

namespace Mud.Server.Tests.Mocking
{
    public class WorldMock : IWorld
    {
        private readonly List<ICharacter> _characters;
        private readonly List<IRoom> _rooms;
        private readonly List<IItem> _items;

        public WorldMock()
        {
            _characters = new List<ICharacter>();
            _rooms = new List<IRoom>();
            _items = new List<IItem>();
        }

        #region IWorld
        
        public IReadOnlyCollection<IRoom> GetRooms()
        {
            return _rooms.AsReadOnly();
        }

        public IReadOnlyCollection<ICharacter> GetCharacters()
        {
            return _characters.AsReadOnly();
        }

        public IReadOnlyCollection<IItem> GetItems()
        {
            return _items.AsReadOnly();
        }

        public ICharacter AddCharacter(Guid guid, CharacterBlueprint blueprint, IRoom room)
        {
            throw new NotImplementedException();
        }

        public IItemContainer AddItemContainer(Guid guid, ItemContainerBlueprint blueprint, IContainer container)
        {
            throw new NotImplementedException();
        }

        public IItemArmor AddItemArmor(Guid guid, ItemArmorBlueprint blueprint, IContainer container)
        {
            throw new NotImplementedException();
        }

        public IItemWeapon AddItemWeapon(Guid guid, ItemWeaponBlueprint blueprint, IContainer container)
        {
            throw new NotImplementedException();
        }

        public IItemLight AddItemLight(Guid guid, ItemLightBlueprint blueprint, IContainer container)
        {
            throw new NotImplementedException();
        }

        public IItemCorpse AddItemCorpse(Guid guid, ItemCorpseBlueprint blueprint, IRoom container, ICharacter victim)
        {
            throw new NotImplementedException();
        }

        public IPeriodicAura AddPeriodicAura(ICharacter victim, string name, ICharacter source, int amount, AmountOperators amountOperator, bool tickVisible, int tickDelay, int totalTicks)
        {
            throw new NotImplementedException();
        }

        public IAura AddAura(ICharacter victim, string name, AuraModifiers modifier, int amount, AmountOperators amountOperator, int totalSeconds, bool visible)
        {
            throw new NotImplementedException();
        }

        public IPeriodicAura AddPeriodicAura(ICharacter victim, string name, ICharacter source, SchoolTypes school, int amount, AmountOperators amountOperator, bool tickVisible, int tickDelay, int totalTicks)
        {
            throw new NotImplementedException();
        }

        public void RemoveCharacter(ICharacter character)
        {
            throw new NotImplementedException();
        }

        public void RemoveItem(IItem item)
        {
            throw new NotImplementedException();
        }

        public void RemoveRoom(IRoom room)
        {
            throw new NotImplementedException();
        }

        public IExit AddExit(IRoom @from, IRoom to, ServerOptions.ExitDirections direction, bool bidirectional)
        {
            throw new NotImplementedException();
        }

        public ICharacter AddCharacter(Guid guid, string name, IRoom room)
        {
            ICharacter character = new Character.Character(guid, name, room);
            _characters.Add(character);
            return character;
        }

        public IRoom AddRoom(Guid guid, string name)
        {
            IRoom room = new Room.Room(guid, name);
            _rooms.Add(room);
            return room;
        }


        public IRoom AddRoom(Guid guid, RoomBlueprint blueprint)
        {
            throw new NotImplementedException();
        }

        public IItem AddItemContainer(Guid guid, ItemBlueprintBase blueprint, IContainer container)
        {
            throw new NotImplementedException();
        }

        public IItem AddItemArmor(Guid guid, ItemBlueprintBase blueprint, IContainer container)
        {
            throw new NotImplementedException();
        }

        public IItem AddItemWeapon(Guid guid, ItemBlueprintBase blueprint, IContainer container)
        {
            throw new NotImplementedException();
        }

        public IItem AddItemLight(Guid guid, ItemBlueprintBase blueprint, IContainer container)
        {
            throw new NotImplementedException();
        }

        public ICharacter GetCharacter(CommandParameter parameter, bool perfectMatch = false)
        {
            return FindHelpers.FindByName(_characters, parameter);
        }

        public void Update()
        {
            // TODO
        }

        #endregion
    }
}
