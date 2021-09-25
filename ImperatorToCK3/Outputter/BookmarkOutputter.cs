using commonItems;
using ImageMagick;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Map;
using ImperatorToCK3.CK3.Titles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.Outputter {
	public static class BookmarkOutputter {
		public static void OutputBookmark(Dictionary<string, Character> characters, Dictionary<string, Title> titles, Configuration config) {
			OpenCL.IsEnabled = true; // enable OpenCL in ImageMagick
			var path = "output/" + config.OutputModName + "/common/bookmarks/00_bookmarks.txt";
			using var stream = File.OpenWrite(path);
			using var output = new StreamWriter(stream, Encoding.UTF8);

			// get province positions
			var provincePositionsPath = Path.Combine(config.Ck3Path, "game/gfx/map/map_object_data/building_locators.txt");
			var provincePositions = new Dictionary<ulong, ProvincePosition>();
			var fileParser = new Parser();
			fileParser.RegisterKeyword("game_object_locator", reader => {
				var listParser = new Parser();
				listParser.RegisterKeyword("instances", instancesReader => {
					foreach(var blob in new BlobList(instancesReader).Blobs) {
						var blobReader = new BufferedReader(blob);
						var instance = ProvincePosition.Parse(blobReader);
						provincePositions[instance.Id] = instance;
					}
				});
				listParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
				listParser.ParseStream(reader);
			});
			fileParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
			fileParser.ParseFile(provincePositionsPath);

			output.WriteLine("bm_converted = {");

			output.WriteLine("\tdefault = yes");
			output.WriteLine($"\tstart_date = {config.Ck3BookmarkDate}");
			output.WriteLine("\tis_playable = yes");
			output.WriteLine("\trecommended = yes");

			var playerTitles = new List<Title>(titles.Values.Where(title => title.PlayerCountry));
			foreach (var title in playerTitles) {
				var holderId = title.GetHolderId(config.Ck3BookmarkDate);
				if (holderId == "0") {
					Logger.Warn($"Cannot add player title {title.Name} to bookmark screen: holder is 0!");
					continue;
				}
				var holder = characters[holderId];

				output.WriteLine("\tcharacter = {");

				output.WriteLine($"\t\tname = {holder.Name}");
				output.WriteLine($"\t\tdynasty = {holder.DynastyID}");
				output.WriteLine("\t\tdynasty_splendor_level = 1");
				output.WriteLine($"\t\ttype = {holder.AgeSex}");
				output.WriteLine($"\t\thistory_id = {holder.ID}");
				output.WriteLine($"\t\tbirth = {holder.BirthDate}");
				output.WriteLine($"\t\ttitle = {title.Name}");
				var gov = title.GetGovernment(config.Ck3BookmarkDate);
				if (gov is not null) {
					output.WriteLine($"\t\tgovernment = {gov}");
				}
				output.WriteLine($"\t\tculture = {holder.Culture}");
				output.WriteLine($"\t\treligion = {holder.Religion}");
				output.WriteLine("\t\tdifficulty = \"BOOKMARK_CHARACTER_DIFFICULTY_EASY\"");

				int count = 0;
				double sumX = 0;
				double sumY = 0;
				foreach (ulong provId in GetProvincesInCountry(titles, title, config).Reverse()) {
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
				var outPortraitPath = "output/" + config.OutputModName + "/common/bookmark_portraits/" + $"{holder.Name}.txt";
				File.WriteAllText(outPortraitPath, templateText);
			}

			output.WriteLine("}");

			DrawBookmarkMap(config, playerTitles, titles, characters);
		}

		private static void DrawBookmarkMap(Configuration config, List<Title> playerTitles, Dictionary<string, Title> titles, Dictionary<string, Character> characters) {
			Logger.Info("Drawing bookmark map.");

			string bookmarkMapPath = Path.Combine(config.Ck3Path, "game/gfx/map/terrain/flatmap.dds");
			using var bookmarkMapImage = new MagickImage(bookmarkMapPath);
			bookmarkMapImage.Scale(2160, 1080);
			bookmarkMapImage.Crop(1920, 1080);
			bookmarkMapImage.RePage();

			string provincesMapPath = Path.Combine(config.Ck3Path, "game/map_data/provinces.png");
			using var provincesImage = new MagickImage(provincesMapPath);
			provincesImage.FilterType = FilterType.Point;
			provincesImage.Resize(2160, 1080);
			provincesImage.Crop(1920, 1080);
			provincesImage.RePage();

			var provDefs = new ProvinceDefinitions(config);
			var mapData = new MapData(provincesImage, provDefs, config);

			foreach (var playerTitle in playerTitles) {
				var colorOnMap = playerTitle.Color1 ?? new Color(new[] { 0, 0, 0 });
				var magickColorOnMap = MagickColor.FromRgb((byte)colorOnMap.R, (byte)colorOnMap.G, (byte)colorOnMap.B);
				HashSet<ulong> heldProvinces = GetProvincesInCountry(titles, playerTitle, config);
				// determine which impassable should be be colored by the country
				var provincesToColor = new HashSet<ulong>(heldProvinces);
				foreach (var impassableId in mapData.ColorableImpassableProvinces) {
					if (!mapData.NeighborsDict.TryGetValue(impassableId, out var neighborProvs)) {
						continue;
					}

					var neighborProvsHeldByCountry = new HashSet<ulong>(neighborProvs.Intersect(heldProvinces));
					if ((double)neighborProvsHeldByCountry.Count / neighborProvs.Count >= 0.5) {
						provincesToColor.Add(impassableId);
					}
				}
				var diff = provincesToColor.Count - heldProvinces.Count;
				Logger.Debug($"Colored {diff} impassable provinces with color of {playerTitle.Name}");

				using var copyImage = new MagickImage(provincesImage);
				foreach (var provinceColor in provincesToColor.Select(province => provDefs.ProvinceToDefinitionDict[province].Color))
				{
					// make pixels of the province black
					copyImage.Opaque(provinceColor, MagickColor.FromRgb(0, 0, 0));
				}
				// replace black with title color
				copyImage.Opaque(MagickColor.FromRgb(0, 0, 0), magickColorOnMap);
				// make pixels all colors but the country color transparent
				copyImage.InverseTransparent(magickColorOnMap);

				// create realm highlight file
				var holder = characters[playerTitle.GetHolderId(config.Ck3BookmarkDate)];
				var highlightPath = Path.Combine(
					"output",
					config.OutputModName,
					$"gfx/interface/bookmarks/bm_converted_{holder.Name}.dds"
				);
				copyImage.Write(highlightPath);

				// make country on map semi-transparent
				copyImage.Evaluate(Channels.Alpha, EvaluateOperator.Divide, 2);
				// add the image on top of blank map image
				bookmarkMapImage.Composite(copyImage, Gravity.Center, CompositeOperator.Over);
			}
			var outputPath = Path.Combine("output", config.OutputModName, "gfx/interface/bookmarks/bm_converted.dds");
			bookmarkMapImage.Write(outputPath);
		}

		private static HashSet<ulong> GetProvincesInCountry(Dictionary<string, Title> titles, Title playerTitle, Configuration config) {
			var holderId = playerTitle.GetHolderId(config.Ck3BookmarkDate);
			var heldCounties = new List<Title>(
				titles.Values.Where(t => t.GetHolderId(config.Ck3BookmarkDate) == holderId && t.Rank == TitleRank.county)
			);
			var heldProvinces = new HashSet<ulong>();
			foreach (var county in heldCounties) {
				heldProvinces.UnionWith(county.CountyProvinces);
			}
			return heldProvinces;
		}
	}
}
