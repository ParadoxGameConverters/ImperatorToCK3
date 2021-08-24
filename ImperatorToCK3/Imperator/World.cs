using System;
using System.Collections.Generic;
using System.IO;
using commonItems;
using ImperatorToCK3.Imperator.Genes;
using ImperatorToCK3.Imperator.Pops;
using Mods = System.Collections.Generic.List<commonItems.Mod>;

namespace ImperatorToCK3.Imperator {
	public class World : Parser {
		private Date startDate = new("450.10.1", AUC: true);
		public Date EndDate { get; private set; } = new Date("727.2.17", AUC: true);
		private GameVersion imperatorVersion = new();
		public Mods Mods { get; private set; } = new();
		public SortedSet<string> dlcs = new();
		public Families.Families Families { get; private set; } = new();
		public Characters.Characters Characters { get; private set; } = new();
		private Pops.Pops pops = new();
		public Provinces.Provinces Provinces { get; private set; } = new();
		public Countries.Countries Countries { get; private set; } = new();
		private Genes.GenesDB genesDB = new();

		private enum SaveType { INVALID = 0, PLAINTEXT = 1, COMPRESSED_ENCODED = 2 }

		private class SaveData {
			public SaveType saveType = SaveType.INVALID;
			public int zipStart = 0;
			public string gameState = string.Empty;
		}
		private readonly SaveData saveGame = new();
		public World(Configuration configuration, ConverterVersion converterVersion) {
			Logger.Info("*** Hello Imperator, Roma Invicta! ***");
			ParseGenes(configuration);

			// parse the save
			RegisterRegex(@"\bSAV\w*\b", reader=> { });
			RegisterKeyword("version", reader => {
				var versionString = new SingleString(reader).String;
				imperatorVersion = new GameVersion(versionString);
				Logger.Info("Savegame version: " + versionString);

				if (converterVersion.MinSource > imperatorVersion) {
					Logger.Error("Converter requires a minimum save from v" + converterVersion.MinSource.ToShortString());
					throw new FormatException("Savegame vs converter version mismatch!");
				}
				if (!converterVersion.MaxSource.IsLargerishThan(imperatorVersion)) {
					Logger.Error("Converter requires a maximum save from v" + converterVersion.MaxSource.ToShortString());
					throw new FormatException("Savegame vs converter version mismatch!");
				}
			});
			RegisterKeyword("date", reader => {
				var dateString = new SingleString(reader).String;
				EndDate = new Date(dateString, AUC: true);  // converted to AD
				Logger.Info("Date: " + dateString);
			});
			RegisterKeyword("enabled_dlcs", reader => {
				var theDLCs = new StringList(reader).Strings;
				dlcs.UnionWith(theDLCs);
				foreach (var dlc in dlcs) {
					Logger.Info("Enabled DLC: " + dlc);
				}
			});
			RegisterKeyword("enabled_mods", reader => {
				Logger.Info("Detecting used mods.");
				var modsList = new StringList(reader).Strings;
				Logger.Info("Savegame claims " + modsList.Count + " mods used:");
				Mods incomingMods = new();
				foreach (var modPath in modsList) {
					Logger.Info("Used mod: " + modPath);
					incomingMods.Add(new Mod("", modPath));
				}

				// Let's locate, verify and potentially update those mods immediately.
				ModLoader modLoader = new();
				modLoader.LoadMods(configuration.ImperatorDocPath, incomingMods);
				Mods = modLoader.UsableMods;
			});
			RegisterKeyword("family", reader => {
				Logger.Info("Loading Families");
				Families = Imperator.Families.Families.ParseBloc(reader);
				Logger.Info("Loaded " + Families.StoredFamilies.Count + " families.");
			});
			RegisterKeyword("character", reader => {
				Logger.Info("Loading Characters");
				Characters = Imperator.Characters.Characters.ParseBloc(reader, genesDB);
				Logger.Info("Loaded " + Characters.StoredCharacters.Count + " characters.");
			});
			RegisterKeyword("provinces", reader => {
				Logger.Info("Loading Provinces");
				Provinces = new Provinces.Provinces(reader);
				Logger.Info("Loaded " + Provinces.StoredProvinces.Count + " provinces.");
			});
			RegisterKeyword("country", reader => {
				Logger.Info("Loading Countries");
				Countries = Imperator.Countries.Countries.ParseBloc(reader);
				Logger.Info("Loaded " + Countries.StoredCountries.Count + " countries.");
			});
			RegisterKeyword("population", reader => {
				Logger.Info("Loading Pops");
				pops = new PopsBloc(reader).PopsFromBloc;
				Logger.Info("Loaded " + pops.StoredPops.Count + " pops.");
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			Logger.Info("Verifying Imperator save.");
			VerifySave(configuration.SaveGamePath);
			ProcessSave(configuration.SaveGamePath);

			var gameState = new BufferedReader(saveGame.gameState);
			ParseStream(gameState);
			ClearRegisteredRules();


			Logger.Info("*** Building World ***");

			// Link all the intertwining pointers
			Logger.Info("Linking Characters with Families");
			Characters.LinkFamilies(Families);
			Families.RemoveUnlinkedMembers();
			Logger.Info("Linking Characters with Spouses");
			Characters.LinkSpouses();
			Logger.Info("Linking Characters with Mothers and Fathers");
			Characters.LinkMothersAndFathers();
			Logger.Info("Linking Provinces with Pops");
			Provinces.LinkPops(pops);
			Logger.Info("Linking Provinces with Countries");
			Provinces.LinkCountries(Countries);
			Logger.Info("Linking Countries with Families");
			Countries.LinkFamilies(Families);

			Logger.Info("*** Good-bye Imperator, rest in peace. ***");
		}
		private void ParseGenes(Configuration configuration) {
			genesDB = new GenesDB(Path.Combine(configuration.ImperatorPath, "game/common/genes/00_genes.txt"));
		}
		private void ProcessSave(string saveGamePath) {
			switch (saveGame.saveType) {
				case SaveType.PLAINTEXT:
					Logger.Info("Importing debug_mode Imperator save.");
					ProcessDebugModeSave(saveGamePath);
					break;
				case SaveType.COMPRESSED_ENCODED:
					Logger.Info("Importing regular Imperator save.");
					ProcessCompressedEncodedSave(saveGamePath);
					break;
				case SaveType.INVALID:
					throw new InvalidDataException("Unknown save type.");
			}
		}
		private void VerifySave(string saveGamePath) {
			using var saveStream = File.Open(saveGamePath, FileMode.Open);
			var buffer = new byte[10];
			saveStream.Read(buffer, 0, 4);
			if (buffer[0] != 'S' || buffer[1] != 'A' || buffer[2] != 'V') {
				throw new InvalidDataException("Savefile of unknown type!");
			}

			char ch;
			do { // skip until newline
				ch = (char)saveStream.ReadByte();
			} while (ch != '\n' && ch != '\r');

			var length = saveStream.Length;
			if (length < 65536) {
				throw new InvalidDataException("Savegame seems a bit too small.");
			}

			saveStream.Position = 0;
			var bigBuf = new byte[65536];
			saveStream.Read(bigBuf);
			Logger.Debug(bigBuf.Length.ToString()); // TODO: REMOVE DEBUG
			saveGame.saveType = SaveType.PLAINTEXT;
			for (var i = 0; i < 65533; ++i) {
				if (BitConverter.ToUInt32(bigBuf, i) == 0x04034B50 && BitConverter.ToUInt16(bigBuf, i - 2) == 4) {
					saveGame.zipStart = i;
					saveGame.saveType = SaveType.COMPRESSED_ENCODED;
				}
			}
		}
		private void ProcessDebugModeSave(string saveGamePath) {
			using var saveReader = new StreamReader(File.Open(saveGamePath, FileMode.Open));
			saveGame.gameState = saveReader.ReadToEnd();
		}
		private void ProcessCompressedEncodedSave(string saveGamePath) {
			saveGame.gameState = Helpers.RakalyCaller.ToPlainText(saveGamePath);
		}
	}
}
