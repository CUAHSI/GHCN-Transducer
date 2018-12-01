using System.Collections.Generic;

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
