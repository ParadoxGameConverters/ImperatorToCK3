using commonItems;
using ImageMagick;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.CommonUtils.Map;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = SixLabors.ImageSharp.Color;

namespace ImperatorToCK3.Outputter;

internal static class BookmarkOutputter {
	private const int ScreenWidth = 1920;
	private const int ScreenHeight = 1080;
	private const int PositionMargin = 150;
	private const double MapToScreenScale = (double)1080 / 4096;
	private const double MinCharacterSpacing = 400;
	private const int MaxSeparationIterations = 100;

	public static async Task OutputBookmark(World world, Configuration config, CK3LocDB ck3LocDB) {
		Logger.Info("Creating bookmark...");

		await OutputBookmarkGroup(config);

		var provincePositions = world.MapData.ProvincePositions;

		var sb = new StringBuilder();
		sb.AppendLine("bm_converted = {");

		sb.AppendLine("\tgroup = bm_converted");
		sb.AppendLine($"\tstart_date = {config.CK3BookmarkDate}");
		sb.AppendLine("\tis_playable = yes");
		sb.AppendLine("\trecommended = yes");
		sb.AppendLine("\tweight = { value = 100 }");

		var playerTitles = GetPlayerTitlesForBookmarkScreen(world.LandedTitles, config);
		var characterPositions = GetCharacterPositions(playerTitles, config, provincePositions);
		for (var index = 0; index < playerTitles.Count; ++index) {
			var title = playerTitles[index];
			var holderId = title.GetHolderId(config.CK3BookmarkDate);
			await AddTitleToBookmarkScreen(title, sb, holderId, world.Characters, ck3LocDB, characterPositions[index], config);
		}

		sb.AppendLine("}");

		var path = Path.Combine("output", config.OutputModName, "common/bookmarks/bookmarks/00_bookmarks.txt");
		await using var output = FileHelper.OpenWriteWithRetries(path, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());

		if (config.AsiaExpansionProjectEnabled) {
			// Remove the AEP bookmarks.
			var dummyAEPBookmarksOutputPath = Path.Combine("output", config.OutputModName, "common/bookmarks/bookmarks/00_AEP_bookmarks.txt");
			await using var dummyAEPBookmarksOutput = FileHelper.OpenWriteWithRetries(dummyAEPBookmarksOutputPath, Encoding.UTF8);
			await dummyAEPBookmarksOutput.WriteAsync("# IRToCK3: Removed AEP bookmarks.");
		}

		await DrawBookmarkMap(config, playerTitles, world);
		Logger.IncrementProgress();
	}

	internal static List<Title> GetPlayerTitlesForBookmarkScreen(Title.LandedTitles landedTitles, Configuration config) {
		var playerTitles = new List<Title>(landedTitles.Where(title => title.PlayerCountry));
		foreach (var title in playerTitles.ToArray()) {
			if (title.GetGovernment(config.CK3BookmarkDate) == "republic_government") {
				// Republics are not playable in vanilla CK3.
				playerTitles.Remove(title);
				continue;
			}

			var holderId = title.GetHolderId(config.CK3BookmarkDate);
			if (holderId == "0") {
				Logger.Warn($"Cannot add player title {title} to bookmark screen: holder is 0!");
				playerTitles.Remove(title);
			}
		}

		return playerTitles;
	}

	internal static async Task AddTitleToBookmarkScreen(
		Title title,
		StringBuilder sb,
		string holderId,
		CharacterCollection characters,
		CK3LocDB ck3LocDB,
		(int X, int Y) position,
		Configuration config
	) {
		var holder = characters[holderId];

		// Add character localization for bookmark screen.
		var holderLoc = ck3LocDB.GetOrCreateLocBlock($"bm_converted_{holder.Id}");
		string? holderNameKey = holder.GetName(config.CK3BookmarkDate);
		if (holderNameKey is not null) {
			if (ck3LocDB.TryGetValue(holderNameKey, out var holderNameLoc)) {
				holderLoc.CopyFrom(holderNameLoc);
			} else {
				// Use the raw name key.
				foreach (var language in ConverterGlobals.SupportedLanguages) {
					holderLoc[language] = holderNameKey;
				}
			}
		}
		var subheadingLoc = ck3LocDB.GetOrCreateLocBlock($"bm_converted_{holder.Id}_subheading");
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			subheadingLoc[language] = "$BOOKMARK_SUBHEADING_DEFAULT$";
		}
		var holderDescLoc = ck3LocDB.GetOrCreateLocBlock($"bm_converted_{holder.Id}_desc");
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			holderDescLoc[language] = string.Empty;
		}

		sb.AppendLine("\tcharacter = {");

		sb.AppendLine($"\t\tname = bm_converted_{holder.Id}");
		var dynastyId = holder.GetDynastyId(config.CK3BookmarkDate);
		if (dynastyId is not null) {
			sb.AppendLine($"\t\tdynasty = {dynastyId}");
		}
		sb.AppendLine("\t\tdynasty_splendor_level = 1");
		sb.AppendLine($"\t\ttype = {holder.GetAgeSex(config.CK3BookmarkDate)}");
		sb.AppendLine($"\t\thistory_id = {holder.Id}");
		sb.AppendLine($"\t\tbirth = {holder.BirthDate}");
		sb.AppendLine($"\t\ttitle = {title.Id}");
		var gov = title.GetGovernment(config.CK3BookmarkDate);
		if (gov is not null) {
			sb.AppendLine($"\t\tgovernment = {gov}");
		}

		sb.AppendLine($"\t\tculture = {holder.GetCultureId(config.CK3BookmarkDate)}");
		var faithId = holder.GetFaithId(config.CK3BookmarkDate);
		if (!string.IsNullOrEmpty(faithId)) {
			sb.AppendLine($"\t\treligion={faithId}");
		}
		sb.AppendLine("\t\tdifficulty = \"BOOKMARK_CHARACTER_DIFFICULTY_EASY\"");
		sb.AppendLine($"\t\tposition = {{ {position.X} {position.Y} }}");
		sb.AppendLine("\t\tanimation = personality_rational");

		sb.AppendLine("\t}");

		await OutputBookmarkPortrait(config, holder);
	}

	internal static async Task OutputBookmarkPortrait(Configuration config, Character holder)
	{
		var agesex = holder.GetAgeSex(config.CK3BookmarkDate);
		
		StringBuilder portraitBuilder = new();
		portraitBuilder.AppendLine($"bm_converted_{holder.Id} = {{");
		portraitBuilder.AppendLine($"\ttype = {agesex}");
		portraitBuilder.AppendLine($"\tage = 0.{holder.GetAge(config.CK3BookmarkDate)}");
		portraitBuilder.AppendLine("\tgenes = {");
		var genesStr = holder.DNA is not null ? string.Join('\n', holder.DNA.DNALines) : string.Empty;
		portraitBuilder.AppendLine("\t\t" + genesStr);
		portraitBuilder.AppendLine("\t}");
		portraitBuilder.AppendLine($"\tentity = {{ {agesexToEntityDict[agesex]} }}");
		portraitBuilder.Append('}');
			
		var outPortraitPath = Path.Combine("output", config.OutputModName, $"common/bookmark_portraits/bm_converted_{holder.Id}.txt");
		await File.WriteAllTextAsync(outPortraitPath, portraitBuilder.ToString());
	}

	// Not sure what is the purpose of these values, but all vanilla bookmark portraits have entity entries.
	private static readonly Dictionary<string, string> agesexToEntityDict = new() {
		{"male", "3942081117 3942081117"},
		{"boy", "324034399 616600735"},
		{"female", "3942081117 3942081117"},
		{"girl", "616600735 616600735"},
	};
	
	internal static async Task OutputBookmarkGroup(Configuration config) {
		var path = Path.Combine("output", config.OutputModName, "common/bookmarks/groups/00_bookmark_groups.txt");
		await using var output = FileHelper.OpenWriteWithRetries(path, Encoding.UTF8);

		await output.WriteLineAsync($"bm_converted = {{ default_start_date = {config.CK3BookmarkDate} }}");
	}

	internal static (int X, int Y) GetClampedMeanPosition(Title title, Configuration config, IReadOnlyDictionary<ulong, ProvincePosition> provincePositions) {
		int count = 0;
		double sumX = 0;
		double sumY = 0;
		foreach (ulong provId in title.GetProvincesInCountry(config.CK3BookmarkDate)) {
			if (!provincePositions.TryGetValue(provId, out var pos)) {
				continue;
			}

			sumX += pos.X;
			sumY += pos.Y;
			++count;
		}

		double meanX = Math.Round(sumX / count);
		double meanY = Math.Round(sumY / count);
		int finalX = Math.Clamp((int)(MapToScreenScale * meanX), PositionMargin, ScreenWidth - PositionMargin);
		int finalY = Math.Clamp(ScreenHeight - (int)(MapToScreenScale * meanY), PositionMargin, ScreenHeight - PositionMargin);
		return (finalX, finalY);
	}

	internal static List<(int X, int Y)> GetCharacterPositions(List<Title> playerTitles, Configuration config, IReadOnlyDictionary<ulong, ProvincePosition> provincePositions) {
		var positions = new List<(double X, double Y)>(playerTitles.Count);
		foreach (var title in playerTitles) {
			positions.Add(GetClampedMeanPosition(title, config, provincePositions));
		}

		SeparatePositions(positions);

		var finalPositions = new List<(int X, int Y)>(positions.Count);
		foreach (var (x, y) in positions) {
			finalPositions.Add(((int)Math.Round(x), (int)Math.Round(y)));
		}
		return finalPositions;
	}

	internal static void SeparatePositions(List<(double X, double Y)> positions) {
		for (var iteration = 0; iteration < MaxSeparationIterations; ++iteration) {
			var anyMoved = false;
			for (var i = 0; i < positions.Count; ++i) {
				for (var j = i + 1; j < positions.Count; ++j) {
					var dx = positions[j].X - positions[i].X;
					var dy = positions[j].Y - positions[i].Y;
					var distance = Math.Sqrt((dx * dx) + (dy * dy));
					if (distance >= MinCharacterSpacing) {
						continue;
					}

					var shift = (MinCharacterSpacing - distance) / 2;
					var unitX = distance > 0 ? dx / distance : 1;
					var unitY = distance > 0 ? dy / distance : 0;
					positions[i] = (positions[i].X - (unitX * shift), positions[i].Y - (unitY * shift));
					positions[j] = (positions[j].X + (unitX * shift), positions[j].Y + (unitY * shift));
					positions[i] = ClampToScreen(positions[i]);
					positions[j] = ClampToScreen(positions[j]);
					anyMoved = true;
				}
			}
			if (!anyMoved) {
				return;
			}
		}
	}

	private static (double X, double Y) ClampToScreen((double X, double Y) position) {
		var x = Math.Clamp(position.X, PositionMargin, ScreenWidth - PositionMargin);
		var y = Math.Clamp(position.Y, PositionMargin, ScreenHeight - PositionMargin);
		return (x, y);
	}

	private static async Task DrawBookmarkMap(Configuration config, List<Title> playerTitles, World ck3World) {
		Logger.Info("Drawing bookmark map...");
		var ck3ModFS = ck3World.ModFS;
		var provincesMapPath = ck3ModFS.GetActualFileLocation("map_data/provinces.png");
		if (provincesMapPath is null) {
			throw new FileNotFoundException("provinces.png not found!");
		}
		var flatmapPath = ck3ModFS.GetActualFileLocation("gfx/map/terrain/flat_maps/flatmap.dds");
		if (flatmapPath is null) {
			throw new FileNotFoundException("flatmap.dds not found!");
		}

		SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager.SetEncoder(PngFormat.Instance, new PngEncoder {
			TransparentColorMode = PngTransparentColorMode.Clear,
			ColorType = PngColorType.RgbWithAlpha
		});
		using var provincesImage = await Image.LoadAsync(provincesMapPath);
		provincesImage.Mutate(x =>
			x.Resize(2160, 1080, KnownResamplers.NearestNeighbor)
				.Crop(1920, 1080)
				.BackgroundColor(Color.Transparent)
		);

		using (var flatmapMagickImage = new MagickImage(flatmapPath)) {
			flatmapMagickImage.Scale(2160, 1080);
			flatmapMagickImage.Crop(1920, 1080);
			byte[] flatmapPngBytes = flatmapMagickImage.ToByteArray(MagickFormat.Png);
			await using var flatmapStream = new MemoryStream(flatmapPngBytes);
			using var bookmarkMapImage = await Image.LoadAsync(flatmapStream);

			var mapData = ck3World.MapData;
			var provDefs = mapData.ProvinceDefinitions;

			foreach (var playerTitle in playerTitles) {
				await DrawPlayerTitleOnMap(config, ck3World.Characters, playerTitle, mapData, provincesImage, provDefs, bookmarkMapImage);
			}

			var outputPath = Path.Combine("output", config.OutputModName, "gfx/interface/bookmarks/bm_converted.png");
			await bookmarkMapImage.SaveAsPngAsync(outputPath);
			await ResaveImageAsDDS(outputPath);
		}
	}

	internal static async Task DrawPlayerTitleOnMap(
		Configuration config,
		CharacterCollection characters,
		Title playerTitle,
		MapData mapData,
		Image provincesImage,
		ProvinceDefinitions provDefs,
		Image bookmarkMapImage
	) {
		var colorOnMap = playerTitle.Color1 ?? new commonItems.Colors.Color(0, 0, 0);
		var rgba32ColorOnMap = new Rgba32((byte)colorOnMap.R, (byte)colorOnMap.G, (byte)colorOnMap.B);
		HashSet<ulong> heldProvinces = playerTitle.GetProvincesInCountry(config.CK3BookmarkDate);
		
		// Determine which impassables should be colored by the country.
		HashSet<ulong> provincesToColor = GetImpassableProvincesToColor(mapData, heldProvinces);
		int diff = provincesToColor.Count - heldProvinces.Count;
		Logger.Debug($"Coloring {diff} impassable provinces with color of {playerTitle}...");

		using var realmHighlightImage = provincesImage.CloneAs<Rgba32>();
		var provinceColorSet = new HashSet<Rgba32>(provincesToColor.Count);
		foreach (var provinceId in provincesToColor) {
			if (!provDefs.ProvinceToColorDict.TryGetValue(provinceId, out Rgb24 provinceColor)) {
				continue;
			}
			var rgbaProvinceColor = new Rgba32();
			provinceColor.ToRgba32(ref rgbaProvinceColor);
			provinceColorSet.Add(rgbaProvinceColor);
		}
		ApplyRealmColorMaskInSinglePass(realmHighlightImage, provinceColorSet, rgba32ColorOnMap);

		// Create realm highlight file.
		var holder = characters[playerTitle.GetHolderId(config.CK3BookmarkDate)];
		var highlightPath = Path.Combine(
			"output",
			config.OutputModName,
			$"gfx/interface/bookmarks/bm_converted_bm_converted_{holder.Id}.png"
		);
		await realmHighlightImage.SaveAsPngAsync(highlightPath);
		await ResaveImageAsDDS(highlightPath);

		// Add the image on top of blank map image.
		// Make the realm on map semi-transparent.
		bookmarkMapImage.Mutate(x => x.DrawImage(realmHighlightImage, 0.5f));
	}

	internal static void ApplyRealmColorMaskInSinglePass(Image<Rgba32> image, HashSet<Rgba32> provinceColorSet, Rgba32 realmColor) {
		Rgba32 transparent = Color.Transparent;
		image.ProcessPixelRows(accessor => {
			for (int y = 0; y < image.Height; ++y) {
				var row = accessor.GetRowSpan(y);
				for (int x = 0; x < row.Length; ++x) {
					row[x] = provinceColorSet.Contains(row[x]) ? realmColor : transparent;
				}
			}
		});
	}

	private static FrozenSet<ulong> GetColorableImpassablesExceptMapEdgeProvinces(MapData mapData) {
		return mapData.ColorableImpassableProvinceIds.Except(mapData.MapEdgeProvinceIds).ToFrozenSet();
	}

	internal static HashSet<ulong> GetImpassableProvincesToColor(MapData mapData, HashSet<ulong> heldProvinceIds) {
		var provinceIdsToColor = new HashSet<ulong>(heldProvinceIds);
		var impassableIds = GetColorableImpassablesExceptMapEdgeProvinces(mapData);
		foreach (ulong impassableId in impassableIds) {
			var totalNonImpassableNeighbors = 0;
			var heldNonImpassableNeighbors = 0;
			foreach (var neighborProvinceId in mapData.GetNeighborProvinceIds(impassableId)) {
				if (impassableIds.Contains(neighborProvinceId)) {
					continue;
				}

				++totalNonImpassableNeighbors;
				if (heldProvinceIds.Contains(neighborProvinceId)) {
					++heldNonImpassableNeighbors;
				}
			}

			if (totalNonImpassableNeighbors > 0 && heldNonImpassableNeighbors * 2 > totalNonImpassableNeighbors) {
				// Realm controls more than half of non-impassable neighbors of the impassable.
				provinceIdsToColor.Add(impassableId);
			}
		}

		return provinceIdsToColor;
	}

	private static async Task ResaveImageAsDDS(string imagePath) {
		using (var magickImage = new MagickImage(imagePath)) {
			await magickImage.WriteAsync(Path.ChangeExtension(imagePath, ".dds"));
		}
		FileHelper.DeleteWithRetries(imagePath);
	}
}