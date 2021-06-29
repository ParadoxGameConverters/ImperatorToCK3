#include "Mod.h"
#include "CommonRegexes.h"
#include "Log.h"
#include "ParserHelpers.h"



Imperator::Mod::Mod(std::istream& theStream) {
	registerSetter("name", name);
	registerRegex("path|archive", [this](const std::string& unused, std::istream& theStream) { path = commonItems::getString(theStream); });
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();

	if (!path.empty()) {
		const auto lastDot = path.find_last_of('.');
		if (lastDot != std::string::npos) {
			const auto ending = path.substr(lastDot + 1, path.size());
			compressed = ending == "zip" || ending == "bin";
		}
	}
}
