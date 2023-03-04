using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ImperatorToCK3.CK3.Cultures; 

public class PillarCollection : IdObjectCollection<string, Pillar> {
	public IEnumerable<Pillar> Heritages => this.Where(p => p.Type == "heritage").ToImmutableList();

	public void LoadPillars(ModFilesystem ck3ModFS) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, pillarId) => {
			AddOrReplace(new Pillar(pillarId, reader));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/culture/pillars", ck3ModFS, "txt", true);
	}
}