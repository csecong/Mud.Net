﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mud.Server.Blueprints.Room;

namespace Mud.Server.Room
{
    public class Exit : IExit
    {
        public Exit(ExitBlueprint blueprint, IRoom destination)
        {
            Name = blueprint.Keyword;
            Keywords = Name?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();
            Description = blueprint.Description;
            Destination = destination;
            Blueprint = blueprint;
            CurrentFlags = Blueprint.Flags;
        }

        #region IExit

        public ExitBlueprint Blueprint { get; private set; }

        public string Name { get; }
        public IEnumerable<string> Keywords { get; }
        public string Description { get; }
        public IRoom Destination { get; private set; }
        public ExitFlags CurrentFlags { get; private set; }

        public bool IsDoor => (CurrentFlags & ExitFlags.Door) == ExitFlags.Door;
        public bool IsClosed => (CurrentFlags & ExitFlags.Closed) == ExitFlags.Closed;
        public bool IsLocked => (CurrentFlags & ExitFlags.Locked) == ExitFlags.Locked;
        public bool IsHidden => (CurrentFlags & ExitFlags.Hidden) == ExitFlags.Hidden;

        public void Open()
        {
            RemoveFlags(ExitFlags.Closed);
        }

        public void Close()
        {
            AddFlags(ExitFlags.Closed);
        }

        public void Unlock()
        {
            RemoveFlags(ExitFlags.Locked);
        }

        public void Lock()
        {
            AddFlags(ExitFlags.Locked);
        }

        public void OnRemoved()
        {
            Destination = null;
            Blueprint = null;
        }

        #endregion

        private void AddFlags(ExitFlags flags)
        {
            CurrentFlags |= flags;
        }

        private void RemoveFlags(ExitFlags flags)
        {
            CurrentFlags &= ~flags;
        }
    }
}
