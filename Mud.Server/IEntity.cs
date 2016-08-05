﻿using System;
using System.Collections.Generic;

namespace Mud.Server
{
    public interface IEntity : IActor
    {
        Guid Id { get; }
        bool IsValid { get; } // always true unless entity has been removed from the game
        string Name { get; }
        IEnumerable<string> Keywords { get; }
        // TODO: keywords: List<string> = Name.Split(' ')
        string DisplayName { get; }
        string Description { get; }

        bool Incarnatable { get; }
        IAdmin IncarnatedBy { get; }

        bool ChangeIncarnation(IAdmin admin);

        void OnRemoved(); // called before removing an item from the game
    }
}
