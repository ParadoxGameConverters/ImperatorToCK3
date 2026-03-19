using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Provinces;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ImperatorToCK3.Imperator.Geography;

internal sealed class Area : IIdentifiable<string> {
	public SortedSet<ulong> ProvinceIds { get; } = new();
	public string Id { get; }

	public Area(string id, BufferedReader areaReader, ProvinceCollection provinceCollection) {
		Id = id;
		this.provinceCollection = provinceCollection;

		var parser = new Parser();
		parser.RegisterKeyword("provinces", reader => {
			var provinceIds = reader.GetULongs();
			ProvinceIds.UnionWith(provinceIds);
		});
		parser.IgnoreAndStoreUnregisteredItems(IgnoredKeywords);
		parser.ParseStream(areaReader);
	}

	public bool ContainsProvince(ulong provinceId) {
		return ProvinceIds.Contains(provinceId);
	}

	public IReadOnlySet<Province> Provinces {
		get {
			EnsureProvinceCachesInitialized();
			return cachedProvinces!;
		}
	}

	public bool TryGetProvince(ulong provinceId, out Province province) {
		EnsureProvinceCachesInitialized();
		return provincesById!.TryGetValue(provinceId, out province!);
	}

	private void EnsureProvinceCachesInitialized() {
		if (cachedProvinces is not null) {
			return;
		}

		var cachedProvincesBuilder = ImmutableSortedSet.CreateBuilder<Province>();
		var provincesByIdCache = new Dictionary<ulong, Province>();
		foreach (var provinceId in ProvinceIds) {
			if (!provinceCollection.TryGetValue(provinceId, out var province)) {
				continue;
			}
			cachedProvincesBuilder.Add(province);
			provincesByIdCache[provinceId] = province;
		}

		cachedProvinces = cachedProvincesBuilder.ToImmutable();
		provincesById = provincesByIdCache;
	}

	private readonly ProvinceCollection provinceCollection;
	private ImmutableSortedSet<Province>? cachedProvinces;
	private Dictionary<ulong, Province>? provincesById;
	public static readonly IgnoredKeywordsSet IgnoredKeywords = new();
}