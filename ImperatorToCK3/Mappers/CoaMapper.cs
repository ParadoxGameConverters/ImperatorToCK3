using System.Collections.Generic;
using commonItems;
using System.IO;

namespace ImperatorToCK3.Mappers {
    public class CoaMapper : Parser {
        public CoaMapper(Configuration theConfiguration) {
            var coasPath = Path.Combine(theConfiguration.ImperatorPath, "game", "common", "coat_of_arms", "coat_of_arms");
            var fileNames = SystemUtils.GetAllFilesInFolderRecursive(coasPath);
            Logger.Log(LogLevel.Info, "Parsing CoAs.");
            RegisterKeys();
            foreach (var fileName in fileNames) {
                ParseFile(Path.Combine(coasPath, fileName));
            }
            ClearRegisteredRules();
            Logger.Log(LogLevel.Info, "Loaded " + coasMap.Count + " coats of arms.");
        }
        public CoaMapper(string coaFilePath) {
            RegisterKeys();
            ParseFile(coaFilePath);
            ClearRegisteredRules();
        }
        private void RegisterKeys() {
            RegisterKeyword("template", ParserHelpers.IgnoreItem); // we don't need templates, we need CoAs!
            RegisterRegex(CommonRegexes.Catchall, (reader, flagName) => {
                coasMap.Add(flagName, new StringOfItem(reader).String);
            });
        }

        public string? GetCoaForFlagName(string impFlagName) {
            bool contains = coasMap.TryGetValue(impFlagName, out string? value);
            return contains ? value : null;
        }

        private Dictionary<string, string> coasMap = new();
    }
}
