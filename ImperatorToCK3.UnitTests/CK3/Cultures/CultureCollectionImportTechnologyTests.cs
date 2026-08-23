using commonItems;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using DotLiquid;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Inventions;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CultureCollectionImportTechnologyTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private const string MapFileName = "inventions_to_innovations_map.liquid";
	private const string MapPath = "configurables/" + MapFileName;

	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly InventionsDB inventionsDB = new();
	private static readonly ImperatorRegionMapper irRegionMapper;

	static CultureCollectionImportTechnologyTests() {
		inventionsDB.LoadInventions(irModFS);

		var irProvinces = new ProvinceCollection { new(1), new(2), new(3) };
		AreaCollection areas = new();
		areas.LoadAreas(irModFS, irProvinces);
		irRegionMapper = new ImperatorRegionMapper(areas, new MapData(irModFS));
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());
	}

	private static void WriteInnovationsMapFile(string content) {
		Directory.CreateDirectory("configurables");
		File.WriteAllText(MapPath, content);
	}

	private static void DeleteInnovationsMapFile() {
		if (File.Exists(MapPath)) {
			File.Delete(MapPath);
		}
	}

	private static CultureMapper GetCultureMapper(TestCK3CultureCollection cultures, string mapContent) {
		var reader = new BufferedReader(mapContent);
		return new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);
	}

	private static Country ParseCountry(ulong countryId, string primaryCulture, params int[] activeInventionBooleans) {
		var activeInventionsStr = string.Join(" ", activeInventionBooleans);
		var reader = new BufferedReader(
			$"primary_culture = {primaryCulture}\nactive_inventions = {{ {activeInventionsStr} }}"
		);
		return Country.Parse(reader, countryId);
	}

	private static void ImportTechnology(TestCK3CultureCollection cultures, CountryCollection countries, CultureMapper cultureMapper) {
		var provinceMapper = new ProvinceMapper();
		var liquidVariables = new Hash();
		cultures.ImportTechnology(countries, cultureMapper, provinceMapper, inventionsDB, new LocDB("english"), liquidVariables);
	}

	[Fact]
	public void LinksAndBonusesAreApplied() {
		var cultures = new TestCK3CultureCollection();
		cultures.GenerateTestCulture("roman");
		cultures.AddInnovationId("innovation_garrison");
		cultures.AddInnovationId("innovation_siege");

		var cultureMapper = GetCultureMapper(cultures, "link = { ck3 = roman ir = roman }");

		try {
			WriteInnovationsMapFile("""
				link = { ir = inv_garrison_1 ck3 = innovation_garrison }
				bonus = { ir = inv_siege_1 ck3 = innovation_siege }
				""");

			var countries = new CountryCollection {
				ParseCountry(1, "roman", 1, 1, 0, 0, 0),
			};
			ImportTechnology(cultures, countries, cultureMapper);

			var culture = cultures["roman"];
			Assert.Contains("innovation_garrison", culture.InnovationsFromImperator);
			Assert.Equal(25, (int)culture.InnovationProgressesFromImperator["innovation_siege"]);
		} finally {
			DeleteInnovationsMapFile();
		}
	}

	[Fact]
	public void BonusProgressOf100ResultsInDiscoveredInnovation() {
		var cultures = new TestCK3CultureCollection();
		cultures.GenerateTestCulture("roman");
		cultures.AddInnovationId("innovation_combined");

		var cultureMapper = GetCultureMapper(cultures, "link = { ck3 = roman ir = roman }");

		try {
			WriteInnovationsMapFile("""
				bonus = { ir = inv_garrison_1 ir = inv_siege_1 ir = inv_siege_2 ir = inv_tax_1 ck3 = innovation_combined }
				""");

			var countries = new CountryCollection {
				ParseCountry(1, "roman", 1, 1, 1, 1, 0),
			};
			ImportTechnology(cultures, countries, cultureMapper);

			var culture = cultures["roman"];
			Assert.Contains("innovation_combined", culture.InnovationsFromImperator);
			Assert.DoesNotContain("innovation_combined", culture.InnovationProgressesFromImperator.Keys);
		} finally {
			DeleteInnovationsMapFile();
		}
	}

	[Fact]
	public void InventionsFromMultipleCountriesWithSameCultureAreMerged() {
		var cultures = new TestCK3CultureCollection();
		cultures.GenerateTestCulture("roman");
		cultures.AddInnovationId("innovation_garrison");
		cultures.AddInnovationId("innovation_siege");

		var cultureMapper = GetCultureMapper(cultures, "link = { ck3 = roman ir = roman }");

		try {
			WriteInnovationsMapFile("""
				link = { ir = inv_garrison_1 ck3 = innovation_garrison }
				link = { ir = inv_siege_1 ck3 = innovation_siege }
				""");

			var countries = new CountryCollection {
				ParseCountry(1, "roman", 1, 0, 0, 0, 0),
				ParseCountry(2, "roman", 0, 1, 0, 0, 0),
			};
			ImportTechnology(cultures, countries, cultureMapper);

			var culture = cultures["roman"];
			Assert.Contains("innovation_garrison", culture.InnovationsFromImperator);
			Assert.Contains("innovation_siege", culture.InnovationsFromImperator);
		} finally {
			DeleteInnovationsMapFile();
		}
	}

	[Fact]
	public void InventionsAreImportedSeparatelyForDifferentCultures() {
		var cultures = new TestCK3CultureCollection();
		cultures.GenerateTestCulture("roman");
		cultures.GenerateTestCulture("greek");
		cultures.AddInnovationId("innovation_garrison");
		cultures.AddInnovationId("innovation_tax");

		var cultureMapper = GetCultureMapper(cultures, """
			link = { ck3 = roman ir = roman }
			link = { ck3 = greek ir = greek }
			""");

		try {
			WriteInnovationsMapFile("""
				link = { ir = inv_garrison_1 ck3 = innovation_garrison }
				link = { ir = inv_tax_2 ck3 = innovation_tax }
				""");

			var countries = new CountryCollection {
				ParseCountry(1, "roman", 1, 0, 0, 0, 0),
				ParseCountry(2, "greek", 0, 0, 0, 0, 1),
			};
			ImportTechnology(cultures, countries, cultureMapper);

			Assert.Contains("innovation_garrison", cultures["roman"].InnovationsFromImperator);
			Assert.DoesNotContain("innovation_tax", cultures["roman"].InnovationsFromImperator);
			Assert.Contains("innovation_tax", cultures["greek"].InnovationsFromImperator);
			Assert.DoesNotContain("innovation_garrison", cultures["greek"].InnovationsFromImperator);
		} finally {
			DeleteInnovationsMapFile();
		}
	}

	[Fact]
	public void MissingCultureLogsWarning() {
		// The culture mapper is built with a collection containing "roman",
		// but the collection used for import doesn't contain it.
		var culturesForMapper = new TestCK3CultureCollection();
		culturesForMapper.GenerateTestCulture("roman");
		var cultureMapper = GetCultureMapper(culturesForMapper, "link = { ck3 = roman ir = roman }");

		var cultures = new TestCK3CultureCollection();

		try {
			WriteInnovationsMapFile("link = { ir = inv_garrison_1 ck3 = innovation_garrison }");

			var countries = new CountryCollection {
				ParseCountry(1, "roman", 1, 0, 0, 0, 0),
			};

			var output = new StringWriter();
			Console.SetOut(output);
			ImportTechnology(cultures, countries, cultureMapper);
			var outputString = output.ToString();

			Assert.Contains("[WARN] Can't import technology for culture roman: culture not found in CK3 cultures!", outputString);
		} finally {
			DeleteInnovationsMapFile();
		}
	}

	[Fact]
	public void MappingsToInvalidInnovationsAreIgnored() {
		// "innovation_garrison" is not registered as a valid CK3 innovation ID,
		// so the mapping should be removed.
		var cultures = new TestCK3CultureCollection();
		cultures.GenerateTestCulture("roman");

		var cultureMapper = GetCultureMapper(cultures, "link = { ck3 = roman ir = roman }");

		try {
			WriteInnovationsMapFile("link = { ir = inv_garrison_1 ck3 = innovation_garrison }");

			var countries = new CountryCollection {
				ParseCountry(1, "roman", 1, 0, 0, 0, 0),
			};
			ImportTechnology(cultures, countries, cultureMapper);

			Assert.Empty(cultures["roman"].InnovationsFromImperator);
			Assert.Empty(cultures["roman"].InnovationProgressesFromImperator);
		} finally {
			DeleteInnovationsMapFile();
		}
	}

	[Fact]
	public void CountryWithUnmappedCultureIsSkipped() {
		var cultures = new TestCK3CultureCollection();
		cultures.GenerateTestCulture("roman");
		cultures.AddInnovationId("innovation_garrison");

		var cultureMapper = GetCultureMapper(cultures, "link = { ck3 = roman ir = roman }");

		try {
			WriteInnovationsMapFile("link = { ir = inv_garrison_1 ck3 = innovation_garrison }");

			var countries = new CountryCollection {
				ParseCountry(1, "barbarian", 1, 0, 0, 0, 0),
			};
			ImportTechnology(cultures, countries, cultureMapper);

			Assert.Empty(cultures["roman"].InnovationsFromImperator);
			Assert.Empty(cultures["roman"].InnovationProgressesFromImperator);
		} finally {
			DeleteInnovationsMapFile();
		}
	}
}