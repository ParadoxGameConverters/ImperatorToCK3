using System.Collections.Generic;
using System.IO;
using ImperatorToCK3.CK3.Titles;

namespace ImperatorToCK3.Outputter {
	public static class CoatOfArmsOutputter {
		public static void OutputCoas(string outputModName, Dictionary<string, Title> titles) {
			// dumping all into one file
			var path = "output/" + outputModName + "/common/coat_of_arms/coat_of_arms/fromImperator.txt";
			using var output = new StreamWriter(path);
			foreach (var (titleName, title) in titles) {
				var coa = title.CoA;
				if (coa is not null) {
					output.WriteLine(titleName + coa);
				}
			}
		}
	}
}
