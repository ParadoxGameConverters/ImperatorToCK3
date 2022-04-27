using commonItems;
using commonItems.Localization;
using ImageMagick;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CK3.Map;
using ImperatorToCK3.CK3.Titles;
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

namespace ImperatorToCK3.Outputter {
	public static class BookmarkOutputter {
		public static void OutputBookmark(World world, Configuration config) {
			var path = Path.Combine("output", config.OutputModName, "common/bookmarks/00_bookmarks.txt");
			using var stream = File.OpenWrite(path);
			using var output = new StreamWriter(stream, Encoding.UTF8);

			var provincePositions = world.MapData.ProvincePositions;

			output.WriteLine("bm_converted = {");

			output.WriteLine("\tdefault = yes");
			output.WriteLine($"\tstart_date = {config.CK3BookmarkDate}");
			output.WriteLine("\tis_playable = yes");
			output.WriteLine("\trecommended = yes");

			var playerTitles = new List<Title>(world.LandedTitles.Where(title => title.PlayerCountry));
			var localizations = new Dictionary<string, LocBlock>();
			foreach (var title in playerTitles) {
				var holderId = title.GetHolderId(config.CK3BookmarkDate);
				if (holderId == "0") {
					Logger.Warn($"Cannot add player title {title} to bookmark screen: holder is 0!");
					continue;
				}

				var holder = world.Characters[holderId];

				// Add character localization for bookmark screen.
				localizations.Add($"bm_converted_{holder.Id}", holder.Localizations[holder.Name]);
				var descLocKey = $"bm_converted_{holder.Id}_desc";
				var descLocBlock = new LocBlock(descLocKey, "english") {
					["english"] = string.Empty
				};
				localizations.Add(descLocKey, descLocBlock);

				output.WriteLine("\tcharacter = {");

				output.WriteLine($"\t\tname = bm_converted_{holder.Id}");
				output.WriteLine($"\t\tdynasty = {holder.DynastyId}");
				output.WriteLine("\t\tdynasty_splendor_level = 1");
				output.WriteLine($"\t\ttype = {holder.AgeSex}");
				output.WriteLine($"\t\thistory_id = {holder.Id}");
				output.WriteLine($"\t\tbirth = {holder.BirthDate}");
				output.WriteLine($"\t\ttitle = {title.Id}");
				var gov = title.GetGovernment(config.CK3BookmarkDate);
				if (gov is not null) {
					output.WriteLine($"\t\tgovernment = {gov}");
				}

				output.WriteLine($"\t\tculture = {holder.Culture}");
				output.WriteLine($"\t\treligion = {holder.Religion}");
				output.WriteLine("\t\tdifficulty = \"BOOKMARK_CHARACTER_DIFFICULTY_EASY\"");
				WritePosition(output, title, config, provincePositions);
				output.WriteLine("\t\tanimation = personality_rational");

				output.WriteLine("\t}");

				string templatePath = holder.AgeSex switch {
					"female" => "blankMod/templates/common/bookmark_portraits/female.txt",
					"girl" => "blankMod/templates/common/bookmark_portraits/girl.txt",
					"boy" => "blankMod/templates/common/bookmark_portraits/boy.txt",
					_ => "blankMod/templates/common/bookmark_portraits/male.txt",
				};
				string templateText = File.ReadAllText(templatePath);
				templateText = templateText.Replace("REPLACE_ME_NAME", $"bm_converted_{holder.Id}");
				templateText = templateText.Replace("REPLACE_ME_AGE", holder.Age.ToString());
				var outPortraitPath = Path.Combine("output", config.OutputModName, $"common/bookmark_portraits/bm_converted_{holder.Id}.txt");
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
				english.WriteLine($" {key}: \"{loc["english"]}\"");
				french.WriteLine($" {key}: \"{loc["french"]}\"");
				german.WriteLine($" {key}: \"{loc["german"]}\"");
				russian.WriteLine($" {key}: \"{loc["russian"]}\"");
				simpChinese.WriteLine($" {key}: \"{loc["simp_chinese"]}\"");
				spanish.WriteLine($" {key}: \"{loc["spanish"]}\"");
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
			string provincesMapPath = Path.Combine(config.CK3Path, "game/map_data/provinces.png");
			string flatmapPath = Path.Combine(config.CK3Path, "game/gfx/map/terrain/flatmap.dds");
			const string tmpFlatmapPath = "temp/flatmap.png";

			SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager.SetEncoder(PngFormat.Instance, new PngEncoder {
				TransparentColorMode = PngTransparentColorMode.Clear,
				ColorType = PngColorType.RgbWithAlpha,
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

			Rgba32 black = Color.Black;

			foreach (var playerTitle in playerTitles) {
				var colorOnMap = playerTitle.Color1 ?? new commonItems.Color(0, 0, 0);
				var rgba32ColorOnMap = new Rgba32((byte)colorOnMap.R, (byte)colorOnMap.G, (byte)colorOnMap.B);
				HashSet<ulong> heldProvinces = playerTitle.GetProvincesInCountry(config.CK3BookmarkDate);
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
				Logger.Debug($"Coloring {diff} impassable provinces with color of {playerTitle}...");

				using var realmHighlightImage = provincesImage.CloneAs<Rgba32>();
				foreach (var provinceColor in provincesToColor.Select(
					province => provDefs.ProvinceToColorDict[province])) {
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

			var outputPath = Path.Combine("output", config.OutputModName, "gfx/interface/bookmarks/bm_converted.png");
			bookmarkMapImage.SaveAsPng(outputPath);
			ResaveImageAsDDS(outputPath);
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
}
