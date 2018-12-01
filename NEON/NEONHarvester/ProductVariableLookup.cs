using System.Collections.Generic;

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
