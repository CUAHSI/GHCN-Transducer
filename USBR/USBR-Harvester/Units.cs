using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USBRHarvester
{
	public class Units
	{
		public int UnitsID { get; set; }
		public string UnitsName { get; set; }
		public string UnitsType { get; set; }
		public string UnitsAbbreviation { get; set; }

		public Units(int UnitsID_, string UnitsName_, string UnitsType_, string UnitsAbbreviation_)
		{
			this.UnitsID = UnitsID_;
			this.UnitsName = UnitsName_;
			this.UnitsType = UnitsType_;
			this.UnitsAbbreviation = UnitsAbbreviation_;
		}

        public Units()
        {
        }
    }
}
