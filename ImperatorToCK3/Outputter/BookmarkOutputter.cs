using commonItems;
using ImageMagick;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CK3.Map;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Localization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.Outputter {
	public static class BookmarkOutputter {
		public static void OutputBookmark(World world, Configuration config) {
			var path = Path.Combine("output", config.OutputModName, "common/bookmarks/00_bookmarks.txt");
			using var stream = File.OpenWrite(path);
			using var output = new StreamWriter(stream, Encoding.UTF8);

			var provincePositions = world.MapData.ProvincePositions;

			output.WriteLine("bm_converted = {");

			output.WriteLine("\tdefault = yes");
			output.WriteLine($"\tstart_date = {config.Ck3BookmarkDate}");
			output.WriteLine("\tis_playable = yes");
			output.WriteLine("\trecommended = yes");

			var playerTitles = new List<Title>(world.LandedTitles.Values.Where(title => title.PlayerCountry));
			var localizations = new Dictionary<string, LocBlock>();
			foreach (var title in playerTitles) {
				var holderId = title.GetHolderId(config.Ck3BookmarkDate);
				if (holderId == "0") {
					Logger.Warn($"Cannot add player title {title.Name} to bookmark screen: holder is 0!");
					continue;
				}

				var holder = world.Characters[holderId];

				// Add character localization for bookmark screen.
				localizations.Add($"bm_converted_{holder.Id}", holder.Localizations[holder.Name]);

				output.WriteLine("\tcharacter = {");

				output.WriteLine($"\t\tname = bm_converted{holder.Id}");
				output.WriteLine($"\t\tdynasty = {holder.DynastyId}");
				output.WriteLine("\t\tdynasty_splendor_level = 1");
				output.WriteLine($"\t\ttype = {holder.AgeSex}");
				output.WriteLine($"\t\thistory_id = {holder.Id}");
				output.WriteLine($"\t\tbirth = {holder.BirthDate}");
				output.WriteLine($"\t\ttitle = {title.Name}");
				var gov = title.GetGovernment(config.Ck3BookmarkDate);
				if (gov is not null) {
					output.WriteLine($"\t\tgovernment = {gov}");
				}

				output.WriteLine($"\t\tculture = {holder.Culture}");
				output.WriteLine($"\t\treligion = {holder.Religion}");
				output.WriteLine("\t\tdifficulty = \"BOOKMARK_CHARACTER_DIFFICULTY_EASY\"");
				WritePosition(output, world, title, config, provincePositions);
				output.WriteLine("\t\tanimation = personality_rational");

				output.WriteLine("\t}");

				string templatePath = holder.AgeSex switch {
					"female" => "blankMod/templates/common/bookmark_portraits/female.txt",
					"girl" => "blankMod/templates/common/bookmark_portraits/girl.txt",
					"boy" => "blankMod/templates/common/bookmark_portraits/boy.txt",
					_ => "blankMod/templates/common/bookmark_portraits/male.txt",
				};
				string templateText = File.ReadAllText(templatePath);
				templateText = templateText.Replace("REPLACE_ME_NAME", holder.Name);
				templateText = templateText.Replace("REPLACE_ME_AGE", holder.Age.ToString());
				var outPortraitPath = Path.Combine("output", config.OutputModName, $"common/bookmark_portraits/{holder.Id}.txt");
				File.WriteAllText(outPortraitPath, templateText);
			}

			output.WriteLine("}");

			DrawBookmarkMap(config, playerTitles, world);
			OutputBookmarkLoc(config, localizations);
		}

		private static void OutputBookmarkLoc(Configuration config, IDictionary<string, LocBlock> localizations) {
			var outputName = config.OutputModName;
			using var englishStream = File.OpenWrite(
				$"output/{outputName}/localization/english/converter_bookmark_l_english.yml");
			using var frenchStream = File.OpenWrite(
				$"output/{outputName}/localization/french/converter_bookmark_l_french.yml");
			using var germanStream = File.OpenWrite(
				$"output/{outputName}/localization/german/converter_bookmark_l_german.yml");
			using var russianStream = File.OpenWrite(
				$"output/{outputName}/localization/russian/converter_bookmark_l_russian.yml");
			using var simpChineseStream = File.OpenWrite(
				$"output/{outputName}/localization/spanish/converter_bookmark_l_simp_chinese.yml");
			using var spanishStream = File.OpenWrite(
				$"output/{outputName}/localization/spanish/converter_bookmark_l_spanish.yml");
			using var english = new StreamWriter(englishStream, Encoding.UTF8);
			using var french = new StreamWriter(frenchStream, Encoding.UTF8);
			using var german = new StreamWriter(germanStream, Encoding.UTF8);
			using var russian = new StreamWriter(russianStream, Encoding.UTF8);
			using var simpChinese = new StreamWriter(simpChineseStream, Encoding.UTF8);
			using var spanish = new StreamWriter(spanishStream, Encoding.UTF8);

			english.WriteLine("l_english:");
			french.WriteLine("l_french:");
			german.WriteLine("l_german:");
			russian.WriteLine("l_russian:");
			simpChinese.WriteLine("l_simp_chinese:");
			spanish.WriteLine("l_spanish:");

			// title localization
			foreach (var (key, loc) in localizations) {
				english.WriteLine($" {key}: \"{loc.english}\"");
				french.WriteLine($" {key}: \"{loc.french}\"");
				german.WriteLine($" {key}: \"{loc.german}\"");
				russian.WriteLine($" {key}: \"{loc.russian}\"");
				simpChinese.WriteLine($" {key}: \"{loc.simp_chinese}\"");
				spanish.WriteLine($" {key}: \"{loc.spanish}\"");
			}
		}

		private static void WritePosition(TextWriter output, World world, Title title, Configuration config, IReadOnlyDictionary<ulong, ProvincePosition> provincePositions) {
			int count = 0;
			double sumX = 0;
			double sumY = 0;
			foreach (ulong provId in title.GetProvincesInCountry(world.LandedTitles, config.Ck3BookmarkDate)) {
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
			string provincesMapPath = Path.Combine(config.Ck3Path, "game/map_data/provinces.png");
			string flatmapPath = Path.Combine(config.Ck3Path, "game/gfx/map/terrain/flatmap.dds");
			SystemUtils.TryCreateFolder("temp");
			const string tmpProvincesMapPath = "temp/provinces.tga";
			const string tmpFlatmapPath = "temp/flatmap.png";

			using (var provincesMagickImage = new MagickImage(provincesMapPath)) {
				provincesMagickImage.FilterType = FilterType.Point;
				provincesMagickImage.Resize(2160, 1080);
				provincesMagickImage.Crop(1920, 1080);
				provincesMagickImage.RePage();
				provincesMagickImage.Write(tmpProvincesMapPath);
			}

			using (var flatmapMagickImage = new MagickImage(flatmapPath)) {
				flatmapMagickImage.Scale(2160, 1080);
				flatmapMagickImage.Crop(1920, 1080);
				flatmapMagickImage.RePage();
				flatmapMagickImage.Write(tmpFlatmapPath);
			}

			using var provincesImage = Image.Load(tmpProvincesMapPath);
			using var bookmarkMapImage = Image.Load(tmpFlatmapPath);

			var mapData = ck3World.MapData;
			var provDefs = mapData.ProvinceDefinitions;

			var black = new Rgba32(0, 0, 0, 1);

			foreach (var playerTitle in playerTitles) {
				var colorOnMap = playerTitle.Color1 ?? new commonItems.Color(new[] { 0, 0, 0 });
				var rgba32ColorOnMap = new Rgba32((byte)colorOnMap.R, (byte)colorOnMap.G, (byte)colorOnMap.B);
				HashSet<ulong> heldProvinces =
					playerTitle.GetProvincesInCountry(ck3World.LandedTitles, config.Ck3BookmarkDate);
				// Determine which impassables should be be colored by the country
				var provincesToColor = new HashSet<ulong>(heldProvinces);
				var impassables = mapData.ColorableImpassableProvinces;
				foreach (var impassableId in impassables) {
					if (!mapData.NeighborsDict.TryGetValue(impassableId, out var neighborProvs)) {
						continue;
					}

					var nonImpassableNeighborProvs = new HashSet<ulong>(neighborProvs.Except(impassables));
					if (nonImpassableNeighborProvs.Count == 0) {
						continue;
					}

					var heldNonImpassableNeighborProvs = nonImpassableNeighborProvs.Intersect(heldProvinces);
					if ((double)heldNonImpassableNeighborProvs.Count() / nonImpassableNeighborProvs.Count > 0.5) {
						// Realm controls more than half of non-impassable neighbors of the impassable.
						provincesToColor.Add(impassableId);
					}
				}

				var diff = provincesToColor.Count - heldProvinces.Count;
				Logger.Debug($"Coloring {diff} impassable provinces with color of {playerTitle.Name}...");

				using var realmHighlightImage = provincesImage.CloneAs<Rgba32>();
				foreach (var provinceColor in provincesToColor.Select(
					province => provDefs.ProvinceToColorDict[province])) {
					// Make pixels of the province black.
					var rgbaProvinceColor = new Rgba32(provinceColor.R, provinceColor.G, provinceColor.B);
					ReplaceColorOnImage(realmHighlightImage, rgbaProvinceColor, black);
				}

				// Replace black with title color.
				ReplaceColorOnImage(realmHighlightImage, black, rgba32ColorOnMap);
				// Make pixels all colors but the country color transparent.
				InverseTransparent(realmHighlightImage, rgba32ColorOnMap);

				// Create realm highlight file.
				var holder = ck3World.Characters[playerTitle.GetHolderId(config.Ck3BookmarkDate)];
				var highlightPath = Path.Combine(
					"output",
					config.OutputModName,
					$"gfx/interface/bookmarks/bm_converted_{holder.Id}.png"
				);
				realmHighlightImage.SaveAsPng(highlightPath);
				ResaveImageAsDDS(highlightPath);

				// Add the image on top of blank map image.
				// Make the realm on map semi-transparent.
				bookmarkMapImage.Mutate(x => x.DrawImage(realmHighlightImage, 0.5f));
			}

			var outputPath = Path.Combine("output", config.OutputModName, "gfx/interface/bookmarks/bm_converted.png");
			bookmarkMapImage.SaveAsPng(outputPath);
			ResaveImageAsDDS(outputPath);
		}

		private static void ReplaceColorOnImage(Image<Rgba32> image, Rgba32 sourceColor, Rgba32 targetColor) {
			for (int y = 0; y < image.Height; ++y) {
				Span<Rgba32> pixelRowSpan = image.GetPixelRowSpan(y);
				for (int x = 0; x < image.Width; ++x) {
					if (pixelRowSpan[x].Equals(sourceColor)) {
						pixelRowSpan[x] = targetColor;
					}
				}
			}
		}

		private static void InverseTransparent(Image<Rgba32> image, Rgba32 color) {
			var transparent = new Rgba32(0, 0, 0, 0);
			for (int y = 0; y < image.Height; ++y) {
				Span<Rgba32> pixelRowSpan = image.GetPixelRowSpan(y);
				for (int x = 0; x < image.Width; ++x) {
					if (pixelRowSpan[x].Equals(color)) {
						continue;
					}
					pixelRowSpan[x] = transparent;
				}
			}
		}

		private static void ResaveImageAsDDS(string imagePath) {
			using (var magickImage = new MagickImage(imagePath)) {
				magickImage.Write($"{CommonFunctions.TrimExtension(imagePath)}.dds");
			}
			File.Delete(imagePath);
		}
	}
}
