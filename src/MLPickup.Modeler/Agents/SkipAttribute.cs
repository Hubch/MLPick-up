﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLPickup.Modeler.Agents
{
    using System;

    [AttributeUsage(AttributeTargets.Method)]
    public class SkipAttribute : Attribute
    {
    }
}
