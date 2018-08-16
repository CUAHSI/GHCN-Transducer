using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEONHarvester
{
    /// <summary>
    /// One NEON Product maps to one CUAHSI method and  multiple CUAHSI variables!
    /// </summary>
    class ProductVariableLookup
    {
        public Dictionary<string, Dictionary<string, Variable>> Lookup { get; set; }
    }
}
