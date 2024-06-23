using commonItems;
using commonItems.Localization;
using ImageMagick;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.CommonUtils.Map;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = SixLabors.ImageSharp.Color;

namespace ImperatorToCK3.Outputter;

public static class BookmarkOutputter {
	public static async Task OutputBookmark(World world, Configuration config) {
		Logger.Info("Creating bookmark...");

		await OutputBookmarkGroup(config);

		var path = Path.Combine("output", config.OutputModName, "common/bookmarks/bookmarks/00_bookmarks.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(path, Encoding.UTF8);

		var provincePositions = world.MapData.ProvincePositions;

		await output.WriteLineAsync("bm_converted = {");

		await output.WriteLineAsync("\tgroup = bm_converted");
		await output.WriteLineAsync($"\tstart_date = {config.CK3BookmarkDate}");
		await output.WriteLineAsync("\tis_playable = yes");
		await output.WriteLineAsync("\trecommended = yes");
		await output.WriteLineAsync("\tweight = { value = 100 }");

		var playerTitles = new List<Title>(world.LandedTitles.Where(title => title.PlayerCountry));
		var localizations = new Dictionary<string, LocBlock>();
		foreach (var title in playerTitles.ToList()) {
			if (title.GetGovernment(config.CK3BookmarkDate) == "republic_government") {
				// Republics are not playable in vanilla CK3.
				continue;
			}
			
			var holderId = title.GetHolderId(config.CK3BookmarkDate);
			if (holderId == "0") {
				Logger.Warn($"Cannot add player title {title} to bookmark screen: holder is 0!");
				playerTitles.Remove(title);
				continue;
			}

			await AddTitleToBookmarkScreen(title, output, holderId, world, localizations, provincePositions, config);
		}

		await output.WriteLineAsync("}");

		await Task.WhenAll(
			OutputBookmarkLoc(config, localizations),
			DrawBookmarkMap(config, playerTitles, world)
		);
		Logger.IncrementProgress();
	}

	private static async Task AddTitleToBookmarkScreen(
		Title title,
		StreamWriter output,
		string holderId,
		World world,
		Dictionary<string, LocBlock> localizations,
		IReadOnlyDictionary<ulong, ProvincePosition> provincePositions,
		Configuration config
	) {
		var holder = world.Characters[holderId];

		// Add character localization for bookmark screen.
		var holderLoc = new LocBlock($"bm_converted_{holder.Id}", ConverterGlobals.PrimaryLanguage);
		holderLoc.CopyFrom(holder.Localizations[holder.GetName(config.CK3BookmarkDate)!]);
		localizations.Add(holderLoc.Id, holderLoc);
		var holderDescLoc = new LocBlock($"bm_converted_{holder.Id}_desc", ConverterGlobals.PrimaryLanguage) {
			[ConverterGlobals.PrimaryLanguage] = string.Empty,
		};
		foreach (var language in ConverterGlobals.SecondaryLanguages) {
			holderDescLoc[language] = string.Empty;
		}
		localizations.Add(holderDescLoc.Id, holderDescLoc);

		await output.WriteLineAsync("\tcharacter = {");

		await output.WriteLineAsync($"\t\tname = bm_converted_{holder.Id}");
		var dynastyId = holder.GetDynastyId(config.CK3BookmarkDate);
		if (dynastyId is not null) {
			await output.WriteLineAsync($"\t\tdynasty = {dynastyId}");
		}
		await output.WriteLineAsync("\t\tdynasty_splendor_level = 1");
		await output.WriteLineAsync($"\t\ttype = {holder.GetAgeSex(config.CK3BookmarkDate)}");
		await output.WriteLineAsync($"\t\thistory_id = {holder.Id}");
		await output.WriteLineAsync($"\t\tbirth = {holder.BirthDate}");
		await output.WriteLineAsync($"\t\ttitle = {title.Id}");
		var gov = title.GetGovernment(config.CK3BookmarkDate);
		if (gov is not null) {
			await output.WriteLineAsync($"\t\tgovernment = {gov}");
		}

		await output.WriteLineAsync($"\t\tculture = {holder.GetCultureId(config.CK3BookmarkDate)}");
		var faithId = holder.GetFaithId(config.CK3BookmarkDate);
		if (!string.IsNullOrEmpty(faithId)) {
			await output.WriteLineAsync($"\t\treligion={faithId}");
		}
		await output.WriteLineAsync("\t\tdifficulty = \"BOOKMARK_CHARACTER_DIFFICULTY_EASY\"");
		await WritePosition(output, title, config, provincePositions);
		await output.WriteLineAsync("\t\tanimation = personality_rational");

		await output.WriteLineAsync("\t}");

		string templatePath = holder.GetAgeSex(config.CK3BookmarkDate) switch {
			"female" => "blankMod/templates/common/bookmark_portraits/female.txt",
			"girl" => "blankMod/templates/common/bookmark_portraits/girl.txt",
			"boy" => "blankMod/templates/common/bookmark_portraits/boy.txt",
			_ => "blankMod/templates/common/bookmark_portraits/male.txt"
		};
		string templateText = await File.ReadAllTextAsync(templatePath);

		templateText = templateText.Replace("REPLACE_ME_NAME", $"bm_converted_{holder.Id}");
		templateText = templateText.Replace("REPLACE_ME_AGE", holder.GetAge(config.CK3BookmarkDate).ToString());
		var genesStr = holder.DNA is not null ? string.Join('\n', holder.DNA.DNALines) : string.Empty;
		templateText = templateText.Replace("ADD_GENES", genesStr);
			
		var outPortraitPath = Path.Combine("output", config.OutputModName, $"common/bookmark_portraits/bm_converted_{holder.Id}.txt");
		await File.WriteAllTextAsync(outPortraitPath, templateText);
	}

	private static async Task OutputBookmarkGroup(Configuration config) {
		var path = Path.Combine("output", config.OutputModName, "common/bookmarks/groups/00_bookmark_groups.txt");
		await using var output = FileOpeningHelper.OpenWriteWithRetries(path, Encoding.UTF8);

		await output.WriteLineAsync($"bm_converted = {{ default_start_date = {config.CK3BookmarkDate} }}");
	}

	private static async Task OutputBookmarkLoc(Configuration config, IDictionary<string, LocBlock> localizations) {
		var outputName = config.OutputModName;
		var baseLocPath = Path.Combine("output", outputName, "localization");
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			var locFilePath = Path.Combine(baseLocPath, language, $"converter_bookmark_l_{language}.yml");
			await using var locWriter = FileOpeningHelper.OpenWriteWithRetries(locFilePath, Encoding.UTF8);

			await locWriter.WriteLineAsync($"l_{language}:");

			// title localization
			foreach (var locBlock in localizations.Values) {
				await locWriter.WriteLineAsync(locBlock.GetYmlLocLineForLanguage(language));
			}
		}
	}

	private static async Task WritePosition(TextWriter output, Title title, Configuration config, IReadOnlyDictionary<ulong, ProvincePosition> provincePositions) {
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
		const double scale = (double)1080 / 4096;
		int finalX = (int)(scale * meanX);
		int finalY = 1080 - (int)(scale * meanY);
		await output.WriteLineAsync($"\t\tposition = {{ {finalX} {finalY} }}");
	}

	private static async Task DrawBookmarkMap(Configuration config, List<Title> playerTitles, World ck3World) {
		Logger.Info("Drawing bookmark map...");
		var ck3ModFS = ck3World.ModFS;
		var provincesMapPath = ck3ModFS.GetActualFileLocation("map_data/provinces.png");
		if (provincesMapPath is null) {
			throw new FileNotFoundException($"{nameof(provincesMapPath)} not found!");
		}
		var flatmapPath = ck3ModFS.GetActualFileLocation("gfx/map/terrain/flatmap.dds");
		if (flatmapPath is null) {
			throw new FileNotFoundException($"{nameof(flatmapPath)} not found!");
		}
		const string tmpFlatmapPath = "temp/flatmap.png";

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
			await flatmapMagickImage.WriteAsync(tmpFlatmapPath);
		}

		using var bookmarkMapImage = await Image.LoadAsync(tmpFlatmapPath);

		var mapData = ck3World.MapData;
		var provDefs = mapData.ProvinceDefinitions;

		foreach (var playerTitle in playerTitles) {
			await DrawPlayerTitleOnMap(config, ck3World, playerTitle, mapData, provincesImage, provDefs, bookmarkMapImage);
		}

		var outputPath = Path.Combine("output", config.OutputModName, "gfx/interface/bookmarks/bm_converted.png");
		await bookmarkMapImage.SaveAsPngAsync(outputPath);
		await ResaveImageAsDDS(outputPath);
	}

	private static async Task DrawPlayerTitleOnMap(
		Configuration config, 
		World ck3World, 
		Title playerTitle, 
		MapData mapData,
		Image provincesImage, 
		ProvinceDefinitions provDefs, 
		Image bookmarkMapImage
	) {
		Rgba32 black = Color.Black;
		
		var colorOnMap = playerTitle.Color1 ?? new commonItems.Colors.Color(0, 0, 0);
		var rgba32ColorOnMap = new Rgba32((byte)colorOnMap.R, (byte)colorOnMap.G, (byte)colorOnMap.B);
		ISet<ulong> heldProvinces = playerTitle.GetProvincesInCountry(config.CK3BookmarkDate);
		
		// Determine which impassables should be colored by the country.
		HashSet<ulong> provincesToColor = GetImpassableProvincesToColor(mapData, heldProvinces);
		int diff = provincesToColor.Count - heldProvinces.Count;
		Logger.Debug($"Coloring {diff} impassable provinces with color of {playerTitle}...");

		using var realmHighlightImage = provincesImage.CloneAs<Rgba32>();
		IEnumerable<Rgb24> provinceColors = provincesToColor.Select(provId => provDefs.ProvinceToColorDict[provId]);
		foreach (var provinceColor in provinceColors) {
			// Make pixels of the province black.
			var rgbaProvinceColor = new Rgba32();
			provinceColor.ToRgba32(ref rgbaProvinceColor);
			ReplaceColorOnImage(realmHighlightImage, rgbaProvinceColor, black);
		}

		// Make all non-black pixels transparent.
		InverseTransparent(realmHighlightImage, black);

		// Replace black with title color.
		ReplaceColorOnImage(realmHighlightImage, black, rgba32ColorOnMap);

		// Create realm highlight file.
		var holder = ck3World.Characters[playerTitle.GetHolderId(config.CK3BookmarkDate)];
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

	private static HashSet<ulong> GetColorableImpassablesExceptMapEdgeProvinces(MapData mapData) {
		return mapData.ColorableImpassableProvinceIds.Except(mapData.MapEdgeProvinceIds).ToHashSet();
	}

	private static HashSet<ulong> GetImpassableProvincesToColor(MapData mapData, ISet<ulong> heldProvinceIds) {
		var provinceIdsToColor = new HashSet<ulong>(heldProvinceIds);
		var impassableIds = GetColorableImpassablesExceptMapEdgeProvinces(mapData);
		foreach (ulong impassableId in impassableIds) {
			var nonImpassableNeighborProvIds = mapData.GetNeighborProvinceIds(impassableId)
				.Except(impassableIds)
				.ToHashSet();
			if (nonImpassableNeighborProvIds.Count == 0) {
				continue;
			}

			var heldNonImpassableNeighborProvIds = nonImpassableNeighborProvIds.Intersect(heldProvinceIds);
			if ((double)heldNonImpassableNeighborProvIds.Count() / nonImpassableNeighborProvIds.Count > 0.5) {
				// Realm controls more than half of non-impassable neighbors of the impassable.
				provinceIdsToColor.Add(impassableId);
			}
		}

		return provinceIdsToColor;
	}

	private static void ReplaceColorOnImage(Image<Rgba32> image, Rgba32 sourceColor, Rgba32 targetColor) {
		image.ProcessPixelRows(accessor => {
			for (int y = 0; y < image.Height; ++y) {
				foreach (ref Rgba32 pixel in accessor.GetRowSpan(y)) {
					if (pixel.Equals(sourceColor)) {
						pixel = targetColor;
					}
				}
			}
		});
	}

	private static void InverseTransparent(Image<Rgba32> image, Rgba32 color) {
		Rgba32 transparent = Color.Transparent;
		image.ProcessPixelRows(accessor => {
			for (int y = 0; y < image.Height; ++y) {
				foreach (ref Rgba32 pixel in accessor.GetRowSpan(y)) {
					if (pixel.Equals(color)) {
						continue;
					}
					pixel = transparent;
				}
			}
		});
	}

	private static async Task ResaveImageAsDDS(string imagePath) {
		using (var magickImage = new MagickImage(imagePath)) {
			await magickImage.WriteAsync(Path.ChangeExtension(imagePath, ".dds"));
		}
		File.Delete(imagePath);
	}
}