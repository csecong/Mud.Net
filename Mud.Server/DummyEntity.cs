﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mud.Server.Commands;

namespace Mud.Server
{
    public class DummyEntity : EntityBase
    {
        public DummyEntity(ICommandProcessor processor)
            : base(processor)
        {
        }
    }
}
