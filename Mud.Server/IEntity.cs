﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mud.Server
{
    public interface IEntity
    {
        long Id { get; }
        string Name { get; }
        string Description { get; }
        bool Impersonable { get; }

        IClient ImpersonatedBy { get; set; }
    }
}