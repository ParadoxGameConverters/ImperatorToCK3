using commonItems;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using Open.Collections;
using System.Collections.Frozen;
using System.IO;
using System.Threading.Tasks;
using ZLinq;

namespace ImperatorToCK3.Outputter;

internal static class ProvincesOutputter {
	public static async Task OutputProvinces(
		string outputModPath,
		ProvinceCollection provinces,
		Title.LandedTitles titles
	) {
		Logger.Info("Writing provinces...");

		FrozenSet<ulong> countyCapitalProvinceIds = titles.Counties.AsValueEnumerable()
			.Select(title => title.CapitalBaronyProvinceId)
			.Where(id => id is not null)
			.Select(id => id!.Value)
			.ToFrozenSet();

		// Output provinces to files named after their de jure kingdoms.
		var alreadyOutputtedProvIds = new ConcurrentHashSet<ulong>();

		var deJureKingdoms = titles.GetDeJureKingdoms();
		Parallel.ForEach(deJureKingdoms, kingdom => {
			var sb = new System.Text.StringBuilder();
			foreach (var province in provinces) {
				if (!kingdom.KingdomContainsProvince(province.Id)) {
					continue;
				}

				ProvinceOutputter.WriteProvince(sb, province, countyCapitalProvinceIds.Contains(province.Id));
				alreadyOutputtedProvIds.Add(province.Id);
			}

			var filePath = $"{outputModPath}/history/provinces/{kingdom.Id}.txt";
			using var historyOutput = new StreamWriter(filePath);
			historyOutput.Write(sb.ToString());
		});

		if (alreadyOutputtedProvIds.Count != provinces.Count) {
			var filePath = $"{outputModPath}/history/provinces/onlyDeJureDuchy.txt";
			await using var historyOutput = TextWriter.Synchronized(new StreamWriter(filePath));
			var deJureDuchies = titles.GetDeJureDuchies();
			foreach (var duchy in deJureDuchies) {
				var sb = new System.Text.StringBuilder();

				foreach (var province in provinces) {
					if (alreadyOutputtedProvIds.Contains(province.Id)) {
						continue;
					}

					if (duchy.DuchyContainsProvince(province.Id)) {
						sb.AppendLine($"# {duchy.Id}");
						ProvinceOutputter.WriteProvince(sb, province, countyCapitalProvinceIds.Contains(province.Id));
						alreadyOutputtedProvIds.Add(province.Id);
					}
				}

				if (sb.Length > 0) {
					await historyOutput.WriteAsync(sb.ToString());
				}
			}
		}

		if (alreadyOutputtedProvIds.Count != provinces.Count) {
			await CreateProvinceMappingFile(outputModPath, provinces, alreadyOutputtedProvIds);
		}

		Logger.IncrementProgress();
	}

	private static async Task CreateProvinceMappingFile(string outputModPath, ProvinceCollection provinces, ConcurrentHashSet<ulong> alreadyOutputtedProvinceIds) {
		var mappingsPath = $"{outputModPath}/history/province_mapping/province_mapping.txt";
		await using var mappingsWriter = FileHelper.OpenWriteWithRetries(mappingsPath, System.Text.Encoding.UTF8);
		await using var threadSafeWriter = TextWriter.Synchronized(mappingsWriter);

		foreach (var province in provinces) {
			if (alreadyOutputtedProvinceIds.Contains(province.Id)) {
				continue;
			}

			var baseProvId = province.BaseProvinceId;
			if (baseProvId is null) {
				continue;
			}

			await threadSafeWriter.WriteLineAsync($"{province.Id} = {baseProvId}");
			alreadyOutputtedProvinceIds.Add(province.Id);
		}
	}
}