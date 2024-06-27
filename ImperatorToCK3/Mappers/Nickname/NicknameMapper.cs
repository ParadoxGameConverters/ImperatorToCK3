using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Nickname;

public sealed class NicknameMapper {
	private readonly Dictionary<string, string> impToCK3NicknameMap = new();

	public NicknameMapper() { }
	public NicknameMapper(string filePath) {
		Logger.Info("Parsing nickname mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile(filePath);
		Logger.Info($"Loaded {impToCK3NicknameMap.Count} nickname links.");

		Logger.IncrementProgress();
	}
	public NicknameMapper(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => {
			var mapping = new NicknameMapping(reader);
			if (mapping.CK3Nickname is null) {
				return;
			}

			foreach (var imperatorNickname in mapping.ImperatorNicknames) {
				impToCK3NicknameMap.Add(imperatorNickname, mapping.CK3Nickname);
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public string? GetCK3NicknameForImperatorNickname(string? impNickname) {
		if (impNickname is null) {
			return null;
		}
		return impToCK3NicknameMap.TryGetValue(impNickname, out var ck3Nickname) ? ck3Nickname : null;
	}
}