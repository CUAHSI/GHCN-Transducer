using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEONHarvester
{
    /// <summary>
    /// One NEON Product code maps to one CUAHSI method!
    /// </summary>
    class ProductMethodLookup
    {
        public Dictionary<string, MethodInfo> Lookup { get; set; }
    }
}
