﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using Mud.Server.Constants;

namespace Mud.Datas
{
    [DataContract]
    public class CharacterData
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Race { get; set; }

        [DataMember]
        public string Class { get; set; }

        [DataMember]
        public int Level { get; set; }

        [DataMember]
        public Sex Sex { get; set; }

        [DataMember]
        public Dictionary<PrimaryAttributeTypes, int> PrimaryAttributes { get; set; }

        // TODO auras, equipment, inventory, cooldowns, room
    }
}