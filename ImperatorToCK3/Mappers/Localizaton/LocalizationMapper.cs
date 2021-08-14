using System;
using System.Collections.Generic;
using System.IO;
using commonItems;
using Mods = System.Collections.Generic.List<commonItems.Mod>;

namespace ImperatorToCK3.Mappers.Localizaton {
    public class LocBlock {
        public string english = "";
        public string french = "";
        public string german = "";
        public string russian = "";
        public string simp_chinese = "";
        public string spanish = "";

        // ModifyForEveryLanguage helps remove boilerplate by applying modifyingMethod to every language in the struct
        //
        // For example:
        // nameLocBlock.english.Replace("$ADJ$", baseAdjLocBlock.english);
        // nameLocBlock.french.Replace("$ADJ$", baseAdjLocBlock.french);
        // nameLocBlock.german.Replace("$ADJ$", baseAdjLocBlock.german);
        // nameLocBlock.russian.Replace("$ADJ$", baseAdjLocBlock.russian);
        // nameLocBlock.simp_chinese.Replace("$ADJ$", baseAdjLocBlock.simp_chinese);
        // nameLocBlock.spanish.Replace("$ADJ$", baseAdjLocBlock.spanish);
        //
        // Can be replaced by:
        // nameLocBlock.ModifyForEveryLanguage(baseAdjLocBlock, (baseLoc, modifyingLoc) => {
        //     baseLoc.Replace("$ADJ$", modifyingLoc);
        // });
        void ModifyForEveryLanguage(LocBlock otherLocBlock, Action<string, string> modifyingMethod) {
            modifyingMethod(english, otherLocBlock.english);
            modifyingMethod(french, otherLocBlock.french);
            modifyingMethod(german, otherLocBlock.german);
            modifyingMethod(russian, otherLocBlock.russian);
            modifyingMethod(simp_chinese, otherLocBlock.simp_chinese);
            modifyingMethod(spanish, otherLocBlock.spanish);
        }
        public void SetLocForLanguage(string languageName, string value) {
            switch (languageName) {
                case "english":
                    english = value;
                    break;
                case "french":
                    french = value;
                    break;
                case "german":
                    german = value;
                    break;
                case "russian":
                    russian = value;
                    break;
                case "simp_chinese":
                    simp_chinese = value;
                    break;
                case "spanish":
                    spanish = value;
                    break;
            }
        }
        private void FillMissingLocWithEnglish(ref string language) {
            if (string.IsNullOrEmpty(language)) {
                language = english;
            }
        }
        public void FillMissingLocsWithEnglish() {
            FillMissingLocWithEnglish(ref french);
            FillMissingLocWithEnglish(ref german);
            FillMissingLocWithEnglish(ref russian);
            FillMissingLocWithEnglish(ref simp_chinese);
            FillMissingLocWithEnglish(ref spanish);
        }
    }
    public class LocalizationMapper {
        private readonly Dictionary<string, LocBlock> localizations = new();

        public void ScrapeLocalizations(Configuration configuration, Mods mods) {
            Logger.Log(LogLevel.Info, "Reading Localization");
            var impPath = configuration.ImperatorPath;
            var scrapingPath = Path.Combine(impPath, "game", "localization");
            ScrapeLanguage("english", scrapingPath);
            ScrapeLanguage("french", scrapingPath);
            ScrapeLanguage("german", scrapingPath);
            ScrapeLanguage("russian", scrapingPath);
            ScrapeLanguage("simp_chinese", scrapingPath);
            ScrapeLanguage("spanish", scrapingPath);

            foreach (var mod in mods) {
                var modLocPath = Path.Combine(mod.Path, "localization");
                if (Directory.Exists(modLocPath)) {
                    Logger.Log(LogLevel.Info, "Found some localization in [" + mod.Name + "]");
                    ScrapeLanguage("english", Path.Combine(mod.Path, "localization"));
                    ScrapeLanguage("french", Path.Combine(mod.Path, "localization"));
                    ScrapeLanguage("german", Path.Combine(mod.Path, "localization"));
                    ScrapeLanguage("russian", Path.Combine(mod.Path, "localization"));
                    ScrapeLanguage("simp_chinese", Path.Combine(mod.Path, "localization"));
                    ScrapeLanguage("spanish", Path.Combine(mod.Path, "localization"));
                    ScrapeLanguage("english", Path.Combine(mod.Path, "localization", "replace"));
                    ScrapeLanguage("french", Path.Combine(mod.Path, "localization", "replace"));
                    ScrapeLanguage("german", Path.Combine(mod.Path, "localization", "replace"));
                    ScrapeLanguage("russian", Path.Combine(mod.Path, "localization", "replace"));
                    ScrapeLanguage("simp_chinese", Path.Combine(mod.Path, "localization", "replace"));
                    ScrapeLanguage("spanish", Path.Combine(mod.Path, "localization", "replace"));
                }
            }
            Logger.Log(LogLevel.Info, localizations.Count.ToString() + "localization lines read.");
        }
        private void ScrapeLanguage(string language, string path) {
            var languagePath = Path.Combine(path, language);
            if (!Directory.Exists(languagePath)) {
                return;
            }
            var fileNames = SystemUtils.GetAllFilesInFolderRecursive(languagePath);
            foreach (var fileName in fileNames) {
                var filePath = Path.Combine(languagePath, fileName);
                try {
                    var stream = File.OpenText(filePath);
                    var reader = new BufferedReader(stream);
                    ScrapeStream(reader, language);
                    stream.Close();
                } catch (Exception e) {
                    Logger.Log(LogLevel.Warning, "Could not parse localization file " + filePath + " : " + e);
                }
            }
        }
        public void ScrapeStream(BufferedReader reader, string language) {
            while (!reader.EndOfStream) {
                var (key, loc) = DetermineKeyLocalizationPair(reader.ReadLine());
                if (key is null || loc is null) {
                    continue;
                }

                var gotValue = localizations.TryGetValue(key, out var locBlock);
                if (gotValue) {
                    locBlock.SetLocForLanguage(language, loc);
                } else {
                    var newBlock = new LocBlock();
                    newBlock.SetLocForLanguage(language, loc);
                    localizations.Add(key, newBlock);
                }
            }
        }
        private static KeyValuePair<string?, string?> DetermineKeyLocalizationPair(string? line) {
            if (line == null || line.Length < 4 || line[0] == '#' || line[1] == '#') {
                return new KeyValuePair<string?, string?>();
            }
            var sepLoc = line.IndexOf(':');
            if (sepLoc == -1) {
                return new KeyValuePair<string?, string?>();
            }
            var key = line.Substring(1, sepLoc - 1);
            var newLine = line.Substring(sepLoc + 1);
            var quoteLoc = newLine.IndexOf('\"');
            var quote2Loc = newLine.LastIndexOf('\"');
            if (quoteLoc == -1 || quote2Loc == -1 || quote2Loc - quoteLoc == 0) {
                return new KeyValuePair<string?, string?>(key, null);
            }
            var value = newLine.Substring(quoteLoc + 1, quote2Loc - quoteLoc - 1);
            return new KeyValuePair<string?, string?>(key, value);
        }
        public LocBlock? GetLocBlockForKey(string key) {
            var gotValue = localizations.TryGetValue(key, out var locBlock);
            if (!gotValue) {
                return null;
            }

            if (!string.IsNullOrEmpty(locBlock.english) &&
                (string.IsNullOrEmpty(locBlock.french) ||
                string.IsNullOrEmpty(locBlock.german) ||
                string.IsNullOrEmpty(locBlock.russian) ||
                string.IsNullOrEmpty(locBlock.simp_chinese) ||
                string.IsNullOrEmpty(locBlock.spanish))
            ) {
                var newBlock = locBlock;
                newBlock.FillMissingLocsWithEnglish();
                return newBlock;
            }
            // Either all is well, or we're missing english. Can't do anything about the latter.
            return locBlock;
        }
        public void AddLocalization(string key, LocBlock locBlock) {
            localizations[key] = locBlock;
        }
    }
}
