using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using commonItems.Serialization;
using System.IO;

namespace ImperatorToCK3.CK3.Legends;

public sealed class LegendSeedCollection : ConcurrentIdObjectCollection<string, LegendSeed>, IPDXSerializable {
	public void LoadSeeds(ModFilesystem ck3ModFS) {
		Logger.Info("Loading legend seeds...");
		
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, seedId) => {
			AddOrReplace(new LegendSeed(seedId, reader));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/legends/legend_seeds", ck3ModFS, "txt", true, false, true);
	}

	public void RemoveAnachronisticSeeds(string configurableFilePath) {
		Logger.Info("Removing anachronistic legend seeds...");
		
		var configurableContent = File.ReadAllText(configurableFilePath);
		var seedIdsToRemove = new BufferedReader(configurableContent).GetStrings();
		foreach (var seedId in seedIdsToRemove) {
			Remove(seedId);
		}
	}
	
	public string Serialize(string indent, bool withBraces) {
		var serializedSeeds = new StringWriter();
		foreach (var seed in this) {
			serializedSeeds.WriteLine($"{seed.Id}={seed.Serialize(string.Empty, true)}");
		}
		return serializedSeeds.ToString();
	}
}