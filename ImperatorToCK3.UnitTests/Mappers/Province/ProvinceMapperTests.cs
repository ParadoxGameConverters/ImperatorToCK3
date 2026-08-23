using commonItems.Mods;
using commonItems.Exceptions;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Mappers.Province;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Province;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProvinceMapperTests {
	private const string TestFilesPath = "TestFiles/MapperTests/ProvinceMapper";
	private const string ImperatorRoot = "TestFiles/MapperTests/ProvinceMapper/Imperator/game";
	private const string CK3Root = "TestFiles/MapperTests/ProvinceMapper/CK3/game";
	private static readonly ModFilesystem IRModFS = new(ImperatorRoot, []);
	private static readonly ModFilesystem CK3ModFS = new(CK3Root, []);
	
	[Fact]
	public void EmptyMappingsDefaultToEmpty() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "empty.txt");
		mapper.LoadMappings(mappingsPath);

		Assert.Empty(mapper.GetImperatorProvinceNumbers(1));
	}

	[Fact]
	public void CanLookupImperatorProvinces() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "many_to_many.txt");
		mapper.LoadMappings(mappingsPath);

		Assert.Equal(2, mapper.GetImperatorProvinceNumbers(1).Count);
		Assert.Equal((ulong)2, mapper.GetImperatorProvinceNumbers(1)[0]);
		Assert.Equal((ulong)1, mapper.GetImperatorProvinceNumbers(1)[1]);
		Assert.Equal(2, mapper.GetImperatorProvinceNumbers(2).Count);
		Assert.Equal((ulong)2, mapper.GetImperatorProvinceNumbers(2)[0]);
		Assert.Equal((ulong)1, mapper.GetImperatorProvinceNumbers(2)[1]);
	}

	[Fact]
	public void CanLookupCK3Provinces() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "many_to_many.txt");
		mapper.LoadMappings(mappingsPath);

		Assert.Equal(2, mapper.GetCK3ProvinceNumbers(1).Count);
		Assert.Equal((ulong)2, mapper.GetCK3ProvinceNumbers(1)[0]);
		Assert.Equal((ulong)1, mapper.GetCK3ProvinceNumbers(1)[1]);
		Assert.Equal(2, mapper.GetCK3ProvinceNumbers(2).Count);
		Assert.Equal((ulong)2, mapper.GetCK3ProvinceNumbers(2)[0]);
		Assert.Equal((ulong)1, mapper.GetCK3ProvinceNumbers(2)[1]);
	}

	[Fact]
	public void TypeMismatchesAreDetected() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "type_mismatches.txt");
		mapper.LoadMappings(mappingsPath);

		var irMapData = new MapData(IRModFS);
		var ck3MapData = new MapData(CK3ModFS);

		var output = new StringWriter();
		Console.SetOut(output);
		
		mapper.DetectInvalidMappings(irMapData, ck3MapData);
		string log = output.ToString();
		Assert.Contains("I:R land province 1 is mapped to CK3 water province 2! Fix the province mappings!", log);
		Assert.Contains("I:R water province 2 is mapped to CK3 land province 1! Fix the province mappings!", log);
		Assert.DoesNotContain(" 3 is mapped", log); // Mapping 3 -> 3 has land on both ends, so should not be reported.
	}

	[Fact]
	public void LoadMappingsThrowsWhenNoVersionIsFound() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "no_version.txt");
		Assert.Throws<ConverterException>(() => mapper.LoadMappings(mappingsPath));
	}

	[Fact]
	public void WaterToWaterAndLandToLandMappingsAreValid() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "water_to_water.txt");
		mapper.LoadMappings(mappingsPath);

		var irMapData = new MapData(IRModFS);
		var ck3MapData = new MapData(CK3ModFS);

		var output = new StringWriter();
		Console.SetOut(output);

		mapper.DetectInvalidMappings(irMapData, ck3MapData);
		Assert.DoesNotContain("is mapped to", output.ToString());
	}

	[Fact]
	public void PluralLandToWaterMismatchesAreReported() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "land_to_water_plural.txt");
		mapper.LoadMappings(mappingsPath);

		var irMapData = new MapData(IRModFS);
		var ck3MapData = new MapData(CK3ModFS);

		var output = new StringWriter();
		Console.SetOut(output);

		mapper.DetectInvalidMappings(irMapData, ck3MapData);
		Assert.Contains("I:R land province 1 is mapped to CK3 water provinces 2,9! Fix the province mappings!", output.ToString());
	}

	[Fact]
	public void PluralWaterToLandMismatchesAreReported() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "water_to_land_plural.txt");
		mapper.LoadMappings(mappingsPath);

		var irMapData = new MapData(IRModFS);
		var ck3MapData = new MapData(CK3ModFS);

		var output = new StringWriter();
		Console.SetOut(output);

		mapper.DetectInvalidMappings(irMapData, ck3MapData);
		Assert.Contains("I:R water province 2 is mapped to CK3 land provinces 1,3! Fix the province mappings!", output.ToString());
	}

	[Fact]
	public void CreateMappingsSkipsInvalidLinksAndWarnsOnManyToMany() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "misc_branches.txt");

		var output = new StringWriter();
		Console.SetOut(output);

		mapper.LoadMappings(mappingsPath);

		Assert.Contains("Many-to-many province mapping found: 14, 15 -> 16, 17", output.ToString());
		Assert.DoesNotContain("6 -> 7", output.ToString()); // one-to-one produces no warning
		Assert.DoesNotContain("8 ->", output.ToString()); // one-to-many produces no warning
		Assert.DoesNotContain("-> 13", output.ToString()); // many-to-one produces no warning

		// One-sided links are skipped.
		Assert.Empty(mapper.GetImperatorProvinceNumbers(18));
		Assert.Empty(mapper.GetCK3ProvinceNumbers(19));

		// Zero province IDs are filtered out.
		Assert.Empty(mapper.GetCK3ProvinceNumbers(0));
		Assert.Empty(mapper.GetImperatorProvinceNumbers(0));

		// The remaining combos are mapped correctly.
		Assert.Equal(new List<ulong> { 9, 10 }, mapper.GetCK3ProvinceNumbers(8));
		Assert.Equal(new List<ulong> { 11, 12 }, mapper.GetImperatorProvinceNumbers(13));
		Assert.Equal(new List<ulong> { 16, 17 }, mapper.GetCK3ProvinceNumbers(14));
		Assert.Equal(new List<ulong> { 14, 15 }, mapper.GetImperatorProvinceNumbers(16));
	}
}