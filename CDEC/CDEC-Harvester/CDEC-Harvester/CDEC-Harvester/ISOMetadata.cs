﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDEC_Harvester
{
    class ISOMetadata
    {
        public int MetadataID { get; set; }
        public string TopicCategory { get; set; }
        public string Title { get; set; }
        public string Abstract { get; set; }
        public string ProfileVersion { get; set; }
        public string MetadataLink { get; set; }
    }
}
