﻿using CCWOnline.Management.Models;
using C* Online.Management.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication2
{
    public class ConferenceRepository : CCWOnline.Management.EntityFramework.GenericRepository<AgilentEntities, Conference>
    {
    }
}
