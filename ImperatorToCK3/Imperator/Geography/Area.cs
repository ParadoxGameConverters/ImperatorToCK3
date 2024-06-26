using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Provinces;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ImperatorToCK3.Imperator.Geography;

public sealed class Area : IIdentifiable<string> {
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

	public IReadOnlySet<Province> Provinces => provinceCollection.Where(p => ContainsProvince(p.Id)).ToImmutableSortedSet();

	private readonly ProvinceCollection provinceCollection;
	public static readonly IgnoredKeywordsSet IgnoredKeywords = new();
}