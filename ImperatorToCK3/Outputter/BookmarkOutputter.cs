using commonItems;
using ImageMagick;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Titles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.Outputter {
	public static class BookmarkOutputter {
		private class ProvincePosition {
			public ulong ID;
			public double X;
			public double Y;
			public static ProvincePosition Parse(BufferedReader reader) {
				positionToReturn = new();
				parser.ParseStream(reader);
				return positionToReturn;
			}
			static ProvincePosition() {
				parser.RegisterRegex("id", reader => {
					positionToReturn.ID = ParserHelpers.GetULong(reader);
				});
				parser.RegisterKeyword("position", reader => {
					var positionsList = ParserHelpers.GetDoubles(reader);
					positionToReturn.X = positionsList[0];
					positionToReturn.Y = positionsList[1];
				});
				parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
			}
			private static ProvincePosition positionToReturn = new();
			private static readonly Parser parser = new();
		}
		public static void OutputBookmark(Dictionary<string, Character> characters, Dictionary<string, Title> titles, Configuration config) {
			OpenCL.IsEnabled = true; // enable OpenCL in ImageMagick
			var path = "output/" + config.OutputModName + "/common/bookmarks/00_bookmarks.txt";
			using var stream = File.OpenWrite(path);
			using var output = new StreamWriter(stream, Encoding.UTF8);

			// calculate position for player character
			var provincePositionsPath = Path.Combine(config.Ck3Path, "game/gfx/map/map_object_data/building_locators.txt");
			var provincePositions = new Dictionary<ulong, ProvincePosition>();
			var listParser = new Parser();
			listParser.RegisterRegex(CommonRegexes.Integer, (reader, idString) => {
				var provId = ulong.Parse(idString);
				provincePositions[provId] = ProvincePosition.Parse(reader);
			});
			listParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			listParser.ParseFile(provincePositionsPath);


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

				var count = 0;
				var sumX = 0.0d;
				var sumY = 0.0d;
				Logger.Debug("================================================");
				foreach (var provId in GetProvincesInCountry(titles, title, config).Reverse()) {
						Logger.Debug("PROVINCE : " + provId);
					if (provincePositions.TryGetValue(provId, out var pos)) {
						sumX += pos.X;
						sumY += pos.Y;
						++count;
						Logger.Debug($"{sumX} {sumY} {count}");
					}
				}
				var meanX = (int)Math.Round(sumX / count);
				var meanY = (int)Math.Round(sumY / count);
				output.WriteLine($"\t\tposition = {{ {meanX} {meanY} }}");

				output.WriteLine("\t\tanimation = personality_rational");

				output.WriteLine("\t}");

				string templateText;
				string templatePath = holder.AgeSex switch {
					"female" => "blankMod/templates/common/bookmark_portraits/female.txt",
					"girl" => "blankMod/templates/common/bookmark_portraits/girl.txt",
					"boy" => "blankMod/templates/common/bookmark_portraits/boy.txt",
					_ => "blankMod/templates/common/bookmark_portraits/male.txt",
				};
				templateText = File.ReadAllText(templatePath);
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

			var bookmarkMapPath = Path.Combine(config.Ck3Path, "game/gfx/map/terrain/flatmap.dds");
			using var bookmarkMapImage = new MagickImage(bookmarkMapPath);
			bookmarkMapImage.Scale(2160, 1080);
			bookmarkMapImage.Crop(1920, 1080);
			bookmarkMapImage.RePage();

			var provincesMapPath = Path.Combine(config.Ck3Path, "game/map_data/provinces.png");
			using var provincesImage = new MagickImage(provincesMapPath);
			provincesImage.FilterType = FilterType.Point;
			provincesImage.Resize(2160, 1080);
			provincesImage.Crop(1920, 1080);
			provincesImage.RePage();

			var provDefinitions = new ProvinceDefinitions(config);
			var mapData = new MapData(provincesImage, provDefinitions, config);

			foreach (var playerTitle in playerTitles) {
				var colorOnMap = playerTitle.Color1 ?? new Color(new[] { 0, 0, 0 });
				var magickColorOnMap = MagickColor.FromRgb((byte)colorOnMap.R, (byte)colorOnMap.G, (byte)colorOnMap.B);
				HashSet<ulong> heldProvinces = GetProvincesInCountry(titles, playerTitle, config);
				// determine which impassable should be be colored by the country
				var provincesToColor = new HashSet<ulong>(heldProvinces);
				foreach (var impassableId in mapData.ColorableImpassableProvinces) {
					if (mapData.NeighborsDict.TryGetValue(impassableId, out var neighborProvs)) {
						var neighborProvsHeldByCountry = new HashSet<ulong>(neighborProvs.Intersect(heldProvinces));
						if ((double)neighborProvsHeldByCountry.Count / neighborProvs.Count >= 0.5) {
							provincesToColor.Add(impassableId);
						}
					}
				}
				var diff = provincesToColor.Count - heldProvinces.Count;
				Logger.Debug($"Colored {diff} impassable provinces with color of {playerTitle.Name}");

				using var copyImage = new MagickImage(provincesImage);
				foreach (var province in provincesToColor) {
					var provinceColor = provDefinitions.ProvinceToDefinitionDict[province].Color;
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
				copyImage.Evaluate(Channels.Alpha, EvaluateOperator.Divide, 4);
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

			
			Logger.Debug($"HELD PROVINCES OF {holderId}: " + string.Join(", ", heldProvinces)); // debug
			return heldProvinces;
		}

		private class ProvinceDefinition {
			public ulong ID { get; }
			public MagickColor Color { get; }
			public ProvinceDefinition(ulong id, byte r, byte g, byte b) {
				ID = id;
				Color = MagickColor.FromRgb(r, g, b);
			}
		}

		private class ProvinceDefinitions {
			public Dictionary<MagickColor, ulong> ColorToProvinceDict { get; } = new();
			public SortedDictionary<ulong, ProvinceDefinition> ProvinceToDefinitionDict { get; } = new();
			public ProvinceDefinitions(Configuration config) {
				var definitionsFilePath = Path.Combine(config.Ck3Path, "game/map_data/definition.csv");
				using var fileStream = File.OpenRead(definitionsFilePath);
				using var definitionFileReader = new StreamReader(fileStream);

				definitionFileReader.ReadLine(); // discard first line

				while (!definitionFileReader.EndOfStream) {
					var line = definitionFileReader.ReadLine();
					if (line is null || line.Length < 4 || line[0] == '#' || line[1] == '#') {
						continue;
					}

					try {
						var columns = line.Split(';');
						var id = ulong.Parse(columns[0]);
						var r = byte.Parse(columns[1]);
						var g = byte.Parse(columns[2]);
						var b = byte.Parse(columns[3]);
						var definition = new ProvinceDefinition(id, r, g, b);
						ProvinceToDefinitionDict.Add(definition.ID, definition);
						ColorToProvinceDict[definition.Color] = definition.ID;
					} catch (Exception e) {
						throw new FormatException($"Line: |{line}| is unparseable! Breaking. ({e})");
					}
				}
			}
		}

		private struct Point {
			public int X { get; set; }
			public int Y { get; set; }
			public Point(int x, int y) {
				X = x;
				Y = y;
			}
		}
		private class MapData {
			public SortedDictionary<ulong, HashSet<ulong>> NeighborsDict { get; } = new();
			public HashSet<ulong> ColorableImpassableProvinces { get; } = new();
			public MapData(MagickImage provincesMap, ProvinceDefinitions provinceDefinitions, Configuration config) {
				DetermineNeighbors(provincesMap, provinceDefinitions);
				FindImpassables(config);
			}
			private void DetermineNeighbors(MagickImage provincesMap, ProvinceDefinitions provinceDefinitions) {
				var height = provincesMap.Height;
				var width = provincesMap.Width;
				for (var y = 0; y < height; ++y) {
					for (var x = 0; x < width; ++x) {
						var position = new Point(x, y);

						var centerColor = GetCenterColor(position, provincesMap);
						var aboveColor = GetAboveColor(position, provincesMap);
						var belowColor = GetBelowColor(position, height, provincesMap);
						var leftColor = GetLeftColor(position, provincesMap);
						var rightColor = GetRightColor(position, width, provincesMap);

						if (!centerColor.Equals(aboveColor)) {
							HandleNeighbor(centerColor, aboveColor, provinceDefinitions);
						}
						if (!centerColor.Equals(rightColor)) {
							HandleNeighbor(centerColor, rightColor, provinceDefinitions);
						}
						if (!centerColor.Equals(belowColor)) {
							HandleNeighbor(centerColor, belowColor, provinceDefinitions);
						}
						if (!centerColor.Equals(leftColor)) {
							HandleNeighbor(centerColor, leftColor, provinceDefinitions);
						}
					}
				}
			}
			private void FindImpassables(Configuration config) {
				var filePath = Path.Combine(config.Ck3Path, "game/map_data/default.map");
				var parser = new Parser();
				const string listRegex = "sea_zones|river_provinces|lakes|impassable_mountains|impassable_seas";
				parser.RegisterRegex(listRegex, (reader, keyword) => {
					Parser.GetNextTokenWithoutMatching(reader); // equals sign
					var typeOfGroup = Parser.GetNextTokenWithoutMatching(reader);
					var provIds = ParserHelpers.GetULongs(reader);
					if (keyword == "impassable_mountains") {
						if (typeOfGroup == "RANGE") {
							if (provIds.Count is < 1 or > 2) {
								throw new FormatException("A range of provinces should have 1 or 2 elements!");
							}
							var beginning = provIds[0];
							var end = provIds.Last();
							for (var id = beginning; id <= end; ++id) {
								ColorableImpassableProvinces.Add(id);
							}
						} else { // type is "LIST"
							ColorableImpassableProvinces.UnionWith(provIds);
						}
					}
				});
				parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
				parser.ParseFile(filePath);
			}
			private static MagickColor GetCenterColor(Point position, MagickImage provincesMap) {
				return GetPixelColor(position, provincesMap);
			}
			private static MagickColor GetAboveColor(Point position, MagickImage provincesMap) {
				if (position.Y > 0) {
					--position.Y;
				}
				return GetPixelColor(position, provincesMap);
			}
			private static MagickColor GetBelowColor(Point position, int height, MagickImage provincesMap) {
				if (position.Y < height - 1) {
					++position.Y;
				}
				return GetPixelColor(position, provincesMap);
			}
			private static MagickColor GetLeftColor(Point position, MagickImage provincesMap) {
				if (position.X > 0) {
					--position.X;
				}
				return GetPixelColor(position, provincesMap);
			}
			private static MagickColor GetRightColor(Point position, int width, MagickImage provincesMap) {
				if (position.X < width - 1) {
					++position.X;
				}
				return GetPixelColor(position, provincesMap);
			}
			private static MagickColor GetPixelColor(Point position, MagickImage provincesMap) {
				var pixels = provincesMap.GetPixels();
				var pixel = pixels.GetPixel(position.X, position.Y);
				var color = pixel.ToColor();
				if (color is null) {
					throw new IndexOutOfRangeException($"Cannot get color for position {position.X}, {position.Y}");
				}
				return new MagickColor(color);
			}

			private void HandleNeighbor(
				MagickColor centerColor,
				MagickColor otherColor,
				ProvinceDefinitions provinceDefinitions
			) {
				var centerProvince = provinceDefinitions.ColorToProvinceDict[centerColor];
				var otherProvince = provinceDefinitions.ColorToProvinceDict[otherColor];
				AddNeighbor(centerProvince, otherProvince);
			}

			private void AddNeighbor(ulong mainProvince, ulong neighborProvince) {
				if (NeighborsDict.TryGetValue(mainProvince, out var neighbors)) {
					neighbors.Add(neighborProvince);
				} else {
					NeighborsDict[mainProvince] = new() { neighborProvince };
				}
			}
		}
	}
}
