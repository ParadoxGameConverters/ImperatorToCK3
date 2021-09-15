using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.CK3.Wars {
	public class War {
		Date StartDate;
		Date EndDate;
		List<string> TargetedTitles;
		string CasusBelli;
		List<string> Attackers;
		List<string> Defenders;
		string Claimant;
	}
}
