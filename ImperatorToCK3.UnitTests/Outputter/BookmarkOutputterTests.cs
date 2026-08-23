using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Diplomacy;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class BookmarkOutputterTests {
	private const string OutputModName = "outputModBookmark";
	private static readonly Date ConversionDate = new(867, 1, 1);

	// Province ids used by the generated map fixture.
	private const ulong LandRedId = 1;
	private const ulong LandGreenId = 2;
	private const ulong EdgeImpassableId = 10;
	private const ulong MidImpassableId = 11;
	private const ulong RingImpassableId = 20;
	private const ulong CenterImpassableId = 21;

	private static string CreateTempDir(string name) {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "BookmarkOutputter", name, Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static void TryDeleteDir(string dir) {
		try {
			if (Directory.Exists(dir)) {
				Directory.Delete(dir, recursive: true);
			}
		} catch {
			// Best-effort cleanup only.
		}
	}

	/// <summary>
	/// Creates a minimal CK3-like map root: default.map, definition.csv and provinces.png.
	/// Layout (5x8 pixels):
	/// row0: red green red green red
	/// row1: impEdge green impMid green impEdge
	/// row2: green red green red green
	/// rows3-5: land | impRing impRing impRing | land (row4 middle is impCenter)
	/// row6: red green red green red
	/// row7: green red green red green
	/// </summary>
	private static async Task<string> CreateMapRootAsync(string tempDir) {
		var mapDataDir = Path.Combine(tempDir, "map_data");
		Directory.CreateDirectory(mapDataDir);

		await File.WriteAllTextAsync(Path.Combine(mapDataDir, "default.map"),
			$$"""
			definitions="definition.csv"
			provinces="provinces.png"
			impassable_terrain={ {{EdgeImpassableId}} {{MidImpassableId}} {{RingImpassableId}} {{CenterImpassableId}} }
			""", TestContext.Current.CancellationToken);

		await File.WriteAllTextAsync(Path.Combine(mapDataDir, "definition.csv"),
			string.Join('\n',
				"#province;red;green;blue;name;x",
				$"{LandRedId};255;0;0;land_red;x",
				$"{LandGreenId};0;255;0;land_green;x",
				$"{EdgeImpassableId};10;0;0;imp_edge;x",
				$"{MidImpassableId};11;0;0;imp_mid;x",
				$"{RingImpassableId};20;0;0;imp_ring;x",
				$"{CenterImpassableId};21;0;0;imp_center;x"
			), TestContext.Current.CancellationToken);

		Rgb24[,] pixels = {
			{ new(255, 0, 0), new(0, 255, 0), new(255, 0, 0), new(0, 255, 0), new(255, 0, 0) },
			{ new(10, 0, 0), new(0, 255, 0), new(11, 0, 0), new(0, 255, 0), new(10, 0, 0) },
			{ new(0, 255, 0), new(255, 0, 0), new(0, 255, 0), new(255, 0, 0), new(0, 255, 0) },
			{ new(255, 0, 0), new(20, 0, 0), new(20, 0, 0), new(20, 0, 0), new(255, 0, 0) },
			{ new(0, 255, 0), new(20, 0, 0), new(21, 0, 0), new(20, 0, 0), new(0, 255, 0) },
			{ new(255, 0, 0), new(20, 0, 0), new(20, 0, 0), new(20, 0, 0), new(255, 0, 0) },
			{ new(255, 0, 0), new(0, 255, 0), new(255, 0, 0), new(0, 255, 0), new(255, 0, 0) },
			{ new(0, 255, 0), new(255, 0, 0), new(0, 255, 0), new(255, 0, 0), new(0, 255, 0) }
		};

		using var image = new Image<Rgb24>(5, 8);
		for (var y = 0; y < 8; ++y) {
			for (var x = 0; x < 5; ++x) {
				image[x, y] = pixels[y, x];
			}
		}

		var pngPath = Path.Combine(mapDataDir, "provinces.png");
		await image.SaveAsPngAsync(pngPath, TestContext.Current.CancellationToken);
		return tempDir;
	}

	private static async Task<MapData> CreateMapDataAsync(string mapRoot) {
		await CreateMapRootAsync(mapRoot);
		return new MapData(new ModFilesystem(mapRoot, Array.Empty<Mod>()));
	}

	[Fact]
	public async Task GetImpassablesColorsThoseSurroundedByHeldLand() {
		var tempDir = CreateTempDir("impassables");
		try {
			var mapData = await CreateMapDataAsync(tempDir);

			// Mid impassable has exactly half of its non-impassable neighbors held: not colored.
			// Center impassable is fully surrounded by other colorable impassables: never colored.
			var resultHalf = BookmarkOutputter.GetImpassableProvincesToColor(mapData, [LandRedId]);
			Assert.Equal([LandRedId], resultHalf.OrderBy(p => p));

			// Both neighbors of the mid impassable are held: more than half -> colored.
			var resultAll = BookmarkOutputter.GetImpassableProvincesToColor(mapData, [LandRedId, LandGreenId]);
			Assert.Equal([LandRedId, LandGreenId, MidImpassableId, RingImpassableId], resultAll.OrderBy(p => p));
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task GetImpassablesExcludesMapEdgeProvinces() {
		var tempDir = CreateTempDir("impassables_edge");
		try {
			var mapData = await CreateMapDataAsync(tempDir);

			var resultEmpty = BookmarkOutputter.GetImpassableProvincesToColor(mapData, []);
			Assert.Empty(resultEmpty);

			// The edge impassable borders held land but must never be colored.
			var result = BookmarkOutputter.GetImpassableProvincesToColor(mapData, [LandRedId, LandGreenId]);
			Assert.DoesNotContain(EdgeImpassableId, result);
			Assert.DoesNotContain(CenterImpassableId, result);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public void GetClampedMeanPositionComputesScaledMeanPosition() {
		var landedTitles = new Title.LandedTitles();
		landedTitles.LoadTitles(new BufferedReader("""
			c_rome = {
				b_one = { province = 1024 }
				b_two = { province = 4096 }
			}
			"""), new ColorFactory());
		var cRome = landedTitles["c_rome"];

		var characters = new CharacterCollection();
		var holder = new Character("char_pos", "Pos", new Date(800, 1, 1), characters);
		characters.AddOrReplace(holder);
		cRome.SetHolder(holder, ConversionDate);

		var config = new Configuration { CK3BookmarkDate = ConversionDate };

		// Only one of the two provinces has a known position: the missing one is skipped.
		var positions = new Dictionary<ulong, ProvincePosition> {
			[1024] = ProvincePosition.Parse(new BufferedReader("id=1024 position={ 2048.0 0.0 2048.0 }"))
		};
		Assert.Equal((540, 540), BookmarkOutputter.GetClampedMeanPosition(cRome, config, positions));

		positions[4096] = ProvincePosition.Parse(new BufferedReader("id=4096 position={ 4096.0 0.0 4096.0 }"));
		Assert.Equal((810, 270), BookmarkOutputter.GetClampedMeanPosition(cRome, config, positions));
	}

	[Fact]
	public void GetClampedMeanPositionClampsPositionToScreenMargins() {
		var landedTitles = new Title.LandedTitles();
		landedTitles.LoadTitles(new BufferedReader("""
			c_east = {
				b_east = { province = 111 }
			}
			c_west = {
				b_west = { province = 222 }
			}
			c_north = {
				b_north = { province = 333 }
			}
			c_south = {
				b_south = { province = 444 }
			}
			"""), new ColorFactory());

		var characters = new CharacterCollection();
		var config = new Configuration { CK3BookmarkDate = ConversionDate };
		foreach (var titleId in new[] { "c_east", "c_west", "c_north", "c_south" }) {
			var holder = new Character($"char_{titleId}", titleId, new Date(800, 1, 1), characters);
			characters.AddOrReplace(holder);
			landedTitles[titleId].SetHolder(holder, ConversionDate);
		}

		// Province positions are in the 8192x4096 map space.
		var positions = new Dictionary<ulong, ProvincePosition> {
			[111] = ProvincePosition.Parse(new BufferedReader("id=111 position={ 8192.0 0.0 2048.0 }")),
			[222] = ProvincePosition.Parse(new BufferedReader("id=222 position={ 0.0 0.0 2048.0 }")),
			[333] = ProvincePosition.Parse(new BufferedReader("id=333 position={ 2048.0 0.0 4096.0 }")),
			[444] = ProvincePosition.Parse(new BufferedReader("id=444 position={ 2048.0 0.0 0.0 }"))
		};

		var expectedPositions = new Dictionary<string, (int X, int Y)> {
			["c_east"] = (1770, 540),
			["c_west"] = (150, 540),
			["c_north"] = (540, 150),
			["c_south"] = (540, 930)
		};
		foreach (var (titleId, expectedPosition) in expectedPositions) {
			Assert.Equal(expectedPosition, BookmarkOutputter.GetClampedMeanPosition(landedTitles[titleId], config, positions));
		}
	}

	[Fact]
	public void GetCharacterPositionsSeparatesOverlappingCharacters() {
		var landedTitles = new Title.LandedTitles();
		landedTitles.LoadTitles(new BufferedReader("""
			c_a = {
				b_a = { province = 555 }
			}
			c_b = {
				b_b = { province = 666 }
			}
			c_c = {
				b_c = { province = 777 }
			}
			"""), new ColorFactory());

		var characters = new CharacterCollection();
		var config = new Configuration { CK3BookmarkDate = ConversionDate };
		foreach (var titleId in new[] { "c_a", "c_b", "c_c" }) {
			var holder = new Character($"char_{titleId}", titleId, new Date(800, 1, 1), characters);
			characters.AddOrReplace(holder);
			landedTitles[titleId].SetHolder(holder, ConversionDate);
		}

		// All three realms share the same location.
		var positions = new Dictionary<ulong, ProvincePosition> {
			[555] = ProvincePosition.Parse(new BufferedReader("id=555 position={ 2048.0 0.0 2048.0 }")),
			[666] = ProvincePosition.Parse(new BufferedReader("id=666 position={ 2048.0 0.0 2048.0 }")),
			[777] = ProvincePosition.Parse(new BufferedReader("id=777 position={ 2048.0 0.0 2048.0 }"))
		};

		var playerTitles = new List<Title> { landedTitles["c_a"], landedTitles["c_b"], landedTitles["c_c"] };
		var result = BookmarkOutputter.GetCharacterPositions(playerTitles, config, positions);

		Assert.Equal(3, result.Count);
		foreach (var (x, y) in result) {
			Assert.InRange(x, 150, 1770);
			Assert.InRange(y, 150, 930);
		}
		for (var i = 0; i < result.Count; ++i) {
			for (var j = i + 1; j < result.Count; ++j) {
				var dx = result[i].X - result[j].X;
				var dy = result[i].Y - result[j].Y;
				var distance = Math.Sqrt((dx * dx) + (dy * dy));
				Assert.True(distance >= 400, $"Characters {i} and {j} are too close together.");
			}
		}
	}

	[Fact]
	public async Task OutputBookmarkGroupWritesGroupWithStartDate() {
		var outputDir = Path.Combine("output", OutputModName);
		try {
			Directory.CreateDirectory(Path.Combine(outputDir, "common", "bookmarks", "groups"));

			var config = new Configuration { OutputModName = OutputModName, CK3BookmarkDate = ConversionDate };
			await BookmarkOutputter.OutputBookmarkGroup(config);

			var outputPath = Path.Combine(outputDir, "common", "bookmarks", "groups", "00_bookmark_groups.txt");
			var output = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
			Assert.Equal($"bm_converted = {{ default_start_date = {ConversionDate} }}{Environment.NewLine}", output);
		} finally {
			TryDeleteDir(Path.Combine("output", OutputModName));
		}
	}

	[Fact]
	public async Task OutputBookmarkPortraitHandlesMissingAndPresentDna() {
		try {
			Directory.CreateDirectory(Path.Combine("output", OutputModName, "common", "bookmark_portraits"));
			var config = new Configuration { OutputModName = OutputModName, CK3BookmarkDate = ConversionDate };
			var characters = new CharacterCollection();

			var adultWithoutDna = new Character("char_adult", "Adult", new Date(700, 1, 1), characters);
			await BookmarkOutputter.OutputBookmarkPortrait(config, adultWithoutDna);
			var adultText = await File.ReadAllTextAsync(
				Path.Combine("output", OutputModName, "common", "bookmark_portraits", "bm_converted_char_adult.txt"),
				TestContext.Current.CancellationToken);
			Assert.Contains("type = male", adultText, StringComparison.Ordinal);
			Assert.Contains("age = 0.167", adultText, StringComparison.Ordinal);
			Assert.Contains("entity = { 3942081117 3942081117 }", adultText, StringComparison.Ordinal);

			var child = new Character("char_child", "Child", new Date(862, 1, 1), characters);
			await BookmarkOutputter.OutputBookmarkPortrait(config, child);
			var childText = await File.ReadAllTextAsync(
				Path.Combine("output", OutputModName, "common", "bookmark_portraits", "bm_converted_char_child.txt"),
				TestContext.Current.CancellationToken);
			Assert.Contains("type = boy", childText, StringComparison.Ordinal);
			Assert.Contains("entity = { 324034399 616600735 }", childText, StringComparison.Ordinal);

			var withDna = new Character("char_dna", "Dna", new Date(700, 1, 1), characters) {
				DNA = new DNA("dna_test", [], [], [])
			};
			await BookmarkOutputter.OutputBookmarkPortrait(config, withDna);
			var dnaText = await File.ReadAllTextAsync(
				Path.Combine("output", OutputModName, "common", "bookmark_portraits", "bm_converted_char_dna.txt"),
				TestContext.Current.CancellationToken);
			Assert.Contains("genes = {", dnaText, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(Path.Combine("output", OutputModName));
		}
	}

	private static Title CreateRomeCounty(Character holder) {
		var landedTitles = new Title.LandedTitles();
		landedTitles.LoadTitles(new BufferedReader("""
			c_rome = {
				b_rome = { province = 1024 }
			}
			"""), new ColorFactory());
		var cRome = landedTitles["c_rome"];
		cRome.SetHolder(holder, ConversionDate);
		return cRome;
	}

	[Fact]
	public async Task AddTitleToBookmarkScreenOutputsFullCharacterEntryAndLoc() {
		try {
			Directory.CreateDirectory(Path.Combine("output", OutputModName, "common", "bookmark_portraits"));
			var config = new Configuration { OutputModName = OutputModName, CK3BookmarkDate = ConversionDate };

			var characters = new CharacterCollection();
			var holder = new Character("char_full", "Marcus Augustus", new Date(700, 1, 1), characters);
			characters.AddOrReplace(holder);
			holder.SetDynastyId("dynn_julii", ConversionDate);
			holder.SetFaithId("faith_jupiter", ConversionDate);
			holder.SetCultureId("culture_roman", ConversionDate);

			var cRome = CreateRomeCounty(holder);
			cRome.SetGovernment("feudal_government", ConversionDate);

			var ck3LocDB = new TestCK3LocDB();

			var sb = new StringBuilder();
			await BookmarkOutputter.AddTitleToBookmarkScreen(cRome, sb, holder.Id, characters, ck3LocDB, (540, 540), config);

			var text = sb.ToString();
			Assert.Contains("\tcharacter = {", text, StringComparison.Ordinal);
			Assert.Contains($"name = bm_converted_{holder.Id}", text, StringComparison.Ordinal);
			Assert.Contains("dynasty = dynn_julii", text, StringComparison.Ordinal);
			Assert.Contains("type = male", text, StringComparison.Ordinal);
			Assert.Contains($"history_id = {holder.Id}", text, StringComparison.Ordinal);
			Assert.Contains("title = c_rome", text, StringComparison.Ordinal);
			Assert.Contains("government = feudal_government", text, StringComparison.Ordinal);
			Assert.Contains("culture = culture_roman", text, StringComparison.Ordinal);
			Assert.Contains("religion=faith_jupiter", text, StringComparison.Ordinal);
			Assert.Contains("position = { 540 540 }", text, StringComparison.Ordinal);
			Assert.Contains("animation = personality_rational", text, StringComparison.Ordinal);

			Assert.True(ck3LocDB.TryGetValue($"bm_converted_{holder.Id}", out var nameLoc), "Character bookmark loc block should be created.");
			Assert.Equal("Marcus Augustus", nameLoc["english"]);
			Assert.True(ck3LocDB.TryGetValue($"bm_converted_{holder.Id}_subheading", out var subheadingLoc), "Subheading loc block should be created.");
			Assert.Equal("$BOOKMARK_SUBHEADING_DEFAULT$", subheadingLoc["english"]);

			Assert.True(File.Exists(Path.Combine("output", OutputModName, "common", "bookmark_portraits", $"bm_converted_{holder.Id}.txt")));
		} finally {
			TryDeleteDir(Path.Combine("output", OutputModName));
		}
	}

	[Fact]
	public async Task AddTitleToBookmarkScreenFallsBackToRawNameKeyAndOmitsMissingFields() {
		try {
			Directory.CreateDirectory(Path.Combine("output", OutputModName, "common", "bookmark_portraits"));
			var config = new Configuration { OutputModName = OutputModName, CK3BookmarkDate = ConversionDate };

			var characters = new CharacterCollection();
			var holder = new Character("char_bare", "Bareus", new Date(700, 1, 1), characters);
			characters.AddOrReplace(holder);

			var cRome = CreateRomeCounty(holder);
			var ck3LocDB = new TestCK3LocDB(); // no loc for the character's name key

			var sb = new StringBuilder();
			await BookmarkOutputter.AddTitleToBookmarkScreen(cRome, sb, holder.Id, characters, ck3LocDB, (540, 540), config);

			var text = sb.ToString();
			Assert.DoesNotContain("dynasty =", text, StringComparison.Ordinal);
			Assert.DoesNotContain("government =", text, StringComparison.Ordinal);
			Assert.DoesNotContain("religion=", text, StringComparison.Ordinal);

			Assert.True(ck3LocDB.TryGetValue($"bm_converted_{holder.Id}", out var nameLoc));
			foreach (var language in ConverterGlobals.SupportedLanguages) {
				Assert.Equal("Bareus", nameLoc[language]);
			}
		} finally {
			TryDeleteDir(Path.Combine("output", OutputModName));
		}
	}

	[Fact]
	public async Task AddTitleToBookmarkScreenHandlesUnnamedHolder() {
		try {
			Directory.CreateDirectory(Path.Combine("output", OutputModName, "common", "bookmark_portraits"));
			var config = new Configuration { OutputModName = OutputModName, CK3BookmarkDate = ConversionDate };

			var characters = new CharacterCollection();
			var holder = new Character("char_noname", new BufferedReader("""
				450.1.1 = { birth = yes }
				"""), characters);
			characters.AddOrReplace(holder);

			var cRome = CreateRomeCounty(holder);
			var ck3LocDB = new TestCK3LocDB();

			var sb = new StringBuilder();
			await BookmarkOutputter.AddTitleToBookmarkScreen(cRome, sb, holder.Id, characters, ck3LocDB, (540, 540), config);

			Assert.True(ck3LocDB.TryGetValue($"bm_converted_{holder.Id}", out var nameLoc));
			Assert.True(string.IsNullOrEmpty(nameLoc["english"]));
			Assert.Contains($"history_id = {holder.Id}", sb.ToString(), StringComparison.Ordinal);
		} finally {
			TryDeleteDir(Path.Combine("output", OutputModName));
		}
	}

	private static Title.LandedTitles ImportPlayerCountries(CharacterCollection characters, out CountryCollection countries) {
		var imperatorRoot = "TestFiles/CoatOfArmsOutputterTests/Imperator/game";
		var irModFS = new ModFilesystem(imperatorRoot, Array.Empty<Mod>());
		var irMapData = new MapData(irModFS);
		var areas = new AreaCollection();
		var irRegionMapper = new ImperatorRegionMapper(areas, irMapData);
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());

		var titles = new Title.LandedTitles();
		countries = new CountryCollection();
		foreach (var (id, tag) in new[] { (1uL, "REP"), (2uL, "GUD"), (3uL, "NHO") }) {
			var country = Country.Parse(new BufferedReader($"tag={tag} flag=flag{tag}"), id);
			country.PlayerCountry = true;
			countries.Add(country);
		}

		var ck3Religions = new ReligionCollection(titles);
		titles.ImportImperatorCountries(countries,
			Array.Empty<Dependency>(),
			new TagTitleMapper(),
			new LocDB("english"),
			new TestCK3LocDB(),
			new ProvinceMapper(),
			new CoaMapper(irModFS),
			new GovernmentMapper(ck3GovernmentIds: Array.Empty<string>()),
			new SuccessionLawMapper(),
			new DefiniteFormMapper(),
			new ReligionMapper(ck3Religions, irRegionMapper, new CK3RegionMapper()),
			new CultureMapper(irRegionMapper, new CK3RegionMapper(), new CultureCollection(new ColorFactory(), new PillarCollection(new ColorFactory(), []), [])),
			new NicknameMapper(),
			characters,
			new Date(400, 1, 1),
			new Configuration(),
			new List<KeyValuePair<Country, Dependency?>>(),
			enabledCK3Dlcs: []
		);
		return titles;
	}

	[Fact]
	public void GetPlayerTitlesFiltersRepublicsAndHolderlessCountries() {
		var characters = new CharacterCollection();
		var holder = new Character("char_player", "Player", new Date(700, 1, 1), characters);
		characters.AddOrReplace(holder);

		var titles = ImportPlayerCountries(characters, out _);
		titles["d_IRTOCK3_REP"].SetGovernment("republic_government", ConversionDate);
		titles["d_IRTOCK3_GUD"].SetHolder(holder, ConversionDate);
		// d_IRTOCK3_NHO stays holderless.

		var config = new Configuration { CK3BookmarkDate = ConversionDate };
		var playerTitles = BookmarkOutputter.GetPlayerTitlesForBookmarkScreen(titles, config);

		var ids = playerTitles.Select(t => t.Id).ToList();
		Assert.Equal(["d_IRTOCK3_GUD"], ids);
	}

	[Fact]
	public async Task DrawPlayerTitleOnMapProducesHighlightImages() {
		var tempDir = CreateTempDir("drawmap");
		try {
			var mapRoot = await CreateMapRootAsync(tempDir);
			var mapData = new MapData(new ModFilesystem(mapRoot, Array.Empty<Mod>()));

			var characters = new CharacterCollection();
			var holder = new Character("char_draw", "Draw", new Date(700, 1, 1), characters);
			characters.AddOrReplace(holder);

			var landedTitles = new Title.LandedTitles();
			landedTitles.LoadTitles(new BufferedReader("""
				c_rome = {
					color = { 100 150 200 }
					b_one = { province = 1 }
					b_missing_def = { province = 99 }
				}
				c_plain = {
					b_plain_one = { province = 2 }
				}
				"""), new ColorFactory());
			var cRome = landedTitles["c_rome"];
			cRome.SetHolder(holder, ConversionDate);
			var cPlain = landedTitles["c_plain"]; // no Color1 set: exercises the black fallback color
			cPlain.SetHolder(holder, ConversionDate);

			var provDefs = new ProvinceDefinitions();
			foreach (var (id, r, g, b) in new[] {
					(LandRedId, (byte)255, (byte)0, (byte)0),
					(LandGreenId, (byte)0, (byte)255, (byte)0),
					(EdgeImpassableId, (byte)10, (byte)0, (byte)0),
					(MidImpassableId, (byte)11, (byte)0, (byte)0),
					(RingImpassableId, (byte)20, (byte)0, (byte)0),
					(CenterImpassableId, (byte)21, (byte)0, (byte)0)
				}) {
				provDefs.AddOrReplace(new ProvinceDefinition(id));
				provDefs.ProvinceToColorDict[id] = new Rgb24(r, g, b);
			}

			using var provincesImage = Image.Load<Rgb24>(Path.Combine(mapRoot, "map_data", "provinces.png"));
			using var bookmarkMapImage = new Image<Rgba32>(16, 16, new Rgba32(50, 60, 70));

			var config = new Configuration { OutputModName = OutputModName, CK3BookmarkDate = ConversionDate };
			Directory.CreateDirectory(Path.Combine("output", OutputModName, "gfx", "interface", "bookmarks"));

			await BookmarkOutputter.DrawPlayerTitleOnMap(config, characters, cRome, mapData, provincesImage, provDefs, bookmarkMapImage);
			await BookmarkOutputter.DrawPlayerTitleOnMap(config, characters, cPlain, mapData, provincesImage, provDefs, bookmarkMapImage);

			var bookmarksDir = Path.Combine("output", OutputModName, "gfx", "interface", "bookmarks");
			Assert.True(File.Exists(Path.Combine(bookmarksDir, $"bm_converted_bm_converted_{holder.Id}.dds")), "Realm highlight DDS file should be created.");
			Assert.False(File.Exists(Path.Combine(bookmarksDir, $"bm_converted_bm_converted_{holder.Id}.png")), "Intermediate PNG should be deleted after DDS conversion.");
		} finally {
			TryDeleteDir(Path.Combine("output", OutputModName));
			TryDeleteDir(tempDir);
		}
	}
}
