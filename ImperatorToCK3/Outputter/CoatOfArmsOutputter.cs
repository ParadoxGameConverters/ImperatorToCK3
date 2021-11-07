using ImperatorToCK3.CK3.Titles;
using System.IO;

namespace ImperatorToCK3.Outputter {
	public static class CoatOfArmsOutputter {
		public static void OutputCoas(string outputModName, LandedTitles landedTitles) {
			// dumping all into one file
			var path = $"output/{outputModName}/common/coat_of_arms/coat_of_arms/fromImperator.txt";
			using var output = new StreamWriter(path);
			foreach (var title in landedTitles.StoredTitles) {
				var coa = title.CoA;
				if (coa is not null) {
					output.WriteLine(title.Name + coa);
				}
			}
		}
	}
}
