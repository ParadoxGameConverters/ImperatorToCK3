using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Technology;

public class InnovationMapper {
	private readonly List<InnovationLink> innovationLinks = [];
	private readonly List<InnovationBonus> innovationBonuses = [];
	
	public void LoadLinksAndBonuses(string configurablePath) {
		var parser = new Parser();
		parser.RegisterKeyword("link", reader => innovationLinks.Add(new InnovationLink(reader)));
		parser.RegisterKeyword("bonus", reader => innovationBonuses.Add(new InnovationBonus(reader)));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(configurablePath);
	}

	public IList<string> GetInnovations(IEnumerable<string> irInventions) { // TODO: USE THIS
		var ck3Innovations = new List<string>();
		foreach (var irInvention in irInventions) {
			foreach (var link in innovationLinks) {
				var match = link.Match(irInvention);
				if (match is not null) {
					ck3Innovations.Add(match);
				}
			}
		}
		return ck3Innovations;
	}

	public IDictionary<string, double> GetInnovationProgresses(ICollection<string> irInventions) { // TODO: USE THIS
		Dictionary<string, double> progressesToReturn = [];
		foreach (var bonus in innovationBonuses) {
			var innovationProgress = bonus.GetProgress(irInventions);
			if (!innovationProgress.HasValue) {
				continue;
			}
			
			if (progressesToReturn.TryGetValue(innovationProgress.Value.Key, out double currentValue)) {
				// Only the highest progress should be kept.
				if (currentValue < innovationProgress.Value.Value) {
					progressesToReturn[innovationProgress.Value.Key] = innovationProgress.Value.Value;
				}
			} else {
				progressesToReturn[innovationProgress.Value.Key] = innovationProgress.Value.Value;
			}
		}
		return progressesToReturn;
	}
}
