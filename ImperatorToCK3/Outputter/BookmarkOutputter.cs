using commonItems;
using commonItems.Localization;
using ImageMagick;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CK3.Map;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Color = SixLabors.ImageSharp.Color;

namespace ImperatorToCK3.Outputter;

public static class BookmarkOutputter {
	public static void OutputBookmark(World world, Configuration config) {
		Logger.Info("Creating bookmark...");

		OutputBookmarkGroup(config);
		Logger.IncrementProgress();

		var path = Path.Combine("output", config.OutputModName, "common/bookmarks/bookmarks/00_bookmarks.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(path, Encoding.UTF8);

		var provincePositions = world.MapData.ProvincePositions;

		output.WriteLine("bm_converted = {");

		output.WriteLine("\tgroup = bm_converted");
		output.WriteLine($"\tstart_date = {config.CK3BookmarkDate}");
		output.WriteLine("\tis_playable = yes");
		output.WriteLine("\trecommended = yes");
		output.WriteLine("\tweight = { value = 100 }");

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

			var holder = world.Characters[holderId];

			// Add character localization for bookmark screen.
			var holderLoc = new LocBlock($"bm_converted_{holder.Id}", ConverterGlobals.PrimaryLanguage);
			holderLoc.CopyFrom(holder.Localizations[holder.GetName(config.CK3BookmarkDate)!]);
			localizations.Add(holderLoc.Id, holderLoc);
			var holderDescLoc = new LocBlock($"bm_converted_{holder.Id}_desc", ConverterGlobals.PrimaryLanguage) {
				[ConverterGlobals.PrimaryLanguage] = string.Empty
			};
			foreach (var language in ConverterGlobals.SecondaryLanguages) {
				holderDescLoc[language] = string.Empty;
			}
			localizations.Add(holderDescLoc.Id, holderDescLoc);

			output.WriteLine("\tcharacter = {");

			output.WriteLine($"\t\tname = bm_converted_{holder.Id}");
			var dynastyId = holder.GetDynastyId(config.CK3BookmarkDate);
			if (dynastyId is not null) {
				output.WriteLine($"\t\tdynasty = {dynastyId}");
			}
			output.WriteLine("\t\tdynasty_splendor_level = 1");
			output.WriteLine($"\t\ttype = {holder.GetAgeSex(config.CK3BookmarkDate)}");
			output.WriteLine($"\t\thistory_id = {holder.Id}");
			output.WriteLine($"\t\tbirth = {holder.BirthDate}");
			output.WriteLine($"\t\ttitle = {title.Id}");
			var gov = title.GetGovernment(config.CK3BookmarkDate);
			if (gov is not null) {
				output.WriteLine($"\t\tgovernment = {gov}");
			}

			output.WriteLine($"\t\tculture = {holder.GetCultureId(config.CK3BookmarkDate)}");
			var faithId = holder.GetFaithId(config.CK3BookmarkDate);
			if (!string.IsNullOrEmpty(faithId)) {
				output.WriteLine($"\t\treligion={faithId}");
			}
			output.WriteLine("\t\tdifficulty = \"BOOKMARK_CHARACTER_DIFFICULTY_EASY\"");
			WritePosition(output, title, config, provincePositions.AsReadOnly());
			output.WriteLine("\t\tanimation = personality_rational");

			output.WriteLine("\t}");

			string templatePath = holder.GetAgeSex(config.CK3BookmarkDate) switch {
				"female" => "blankMod/templates/common/bookmark_portraits/female.txt",
				"girl" => "blankMod/templates/common/bookmark_portraits/girl.txt",
				"boy" => "blankMod/templates/common/bookmark_portraits/boy.txt",
				_ => "blankMod/templates/common/bookmark_portraits/male.txt"
			};
			string templateText = File.ReadAllText(templatePath);

			templateText = templateText.Replace("REPLACE_ME_NAME", $"bm_converted_{holder.Id}");
			templateText = templateText.Replace("REPLACE_ME_AGE", holder.GetAge(config.CK3BookmarkDate).ToString());
			var genesStr = holder.DNA is not null ? string.Join("\n", holder.DNA.DNALines) : string.Empty;
			templateText = templateText.Replace("ADD_GENES", genesStr);
			
			var outPortraitPath = Path.Combine("output", config.OutputModName, $"common/bookmark_portraits/bm_converted_{holder.Id}.txt");
			File.WriteAllText(outPortraitPath, templateText);
		}

		output.WriteLine("}");

		OutputBookmarkLoc(config, localizations);
		Logger.IncrementProgress();

		DrawBookmarkMap(config, playerTitles, world);
	}

	private static void OutputBookmarkGroup(Configuration config) {
		var path = Path.Combine("output", config.OutputModName, "common/bookmarks/groups/00_bookmark_groups.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(path, Encoding.UTF8);

		output.WriteLine("bm_converted = {");
		output.WriteLine($"\tdefault_start_date = {config.CK3BookmarkDate}");
		output.WriteLine("}");
	}

	private static void OutputBookmarkLoc(Configuration config, IDictionary<string, LocBlock> localizations) {
		var outputName = config.OutputModName;
		var baseLocPath = Path.Combine("output", outputName, "localization");
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			var locFilePath = Path.Combine(baseLocPath, language, $"converter_bookmark_l_{language}.yml");
			using var locWriter = FileOpeningHelper.OpenWriteWithRetries(locFilePath, Encoding.UTF8);

			locWriter.WriteLine($"l_{language}:");

			// title localization
			foreach (var locBlock in localizations.Values) {
				locWriter.WriteLine(locBlock.GetYmlLocLineForLanguage(language));
			}
		}
	}

	private static void WritePosition(TextWriter output, Title title, Configuration config, IReadOnlyDictionary<ulong, ProvincePosition> provincePositions) {
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
		output.WriteLine($"\t\tposition = {{ {finalX} {finalY} }}");
	}

	private static void DrawBookmarkMap(Configuration config, List<Title> playerTitles, World ck3World) {
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
		using var provincesImage = Image.Load(provincesMapPath);
		provincesImage.Mutate(x =>
			x.Resize(2160, 1080, KnownResamplers.NearestNeighbor)
				.Crop(1920, 1080)
				.BackgroundColor(Color.Transparent)
		);

		using (var flatmapMagickImage = new MagickImage(flatmapPath)) {
			flatmapMagickImage.Scale(2160, 1080);
			flatmapMagickImage.Crop(1920, 1080);
			flatmapMagickImage.Write(tmpFlatmapPath);
		}

		using var bookmarkMapImage = Image.Load(tmpFlatmapPath);

		var mapData = ck3World.MapData;
		var provDefs = mapData.ProvinceDefinitions;

		foreach (var playerTitle in playerTitles) {
			DrawPlayerTitleOnMap(config, ck3World, playerTitle, mapData, provincesImage, provDefs, bookmarkMapImage);
		}

		var outputPath = Path.Combine("output", config.OutputModName, "gfx/interface/bookmarks/bm_converted.png");
		bookmarkMapImage.SaveAsPng(outputPath);
		ResaveImageAsDDS(outputPath);

		Logger.IncrementProgress();
	}

	private static void DrawPlayerTitleOnMap(
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
		
		// Determine which impassables should be be colored by the country
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
		realmHighlightImage.SaveAsPng(highlightPath);
		ResaveImageAsDDS(highlightPath);

		// Add the image on top of blank map image.
		// Make the realm on map semi-transparent.
		bookmarkMapImage.Mutate(x => x.DrawImage(realmHighlightImage, 0.5f));
	}

	private static HashSet<ulong> GetImpassableProvincesToColor(MapData mapData, ISet<ulong> heldProvinceIds) {
		var provinceIdsToColor = new HashSet<ulong>(heldProvinceIds);
		var impassableIds = mapData.ColorableImpassableProvinceIds;
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

	private static void ResaveImageAsDDS(string imagePath) {
		using (var magickImage = new MagickImage(imagePath)) {
			magickImage.Write(Path.ChangeExtension(imagePath, ".dds"));
		}
		File.Delete(imagePath);
	}
}