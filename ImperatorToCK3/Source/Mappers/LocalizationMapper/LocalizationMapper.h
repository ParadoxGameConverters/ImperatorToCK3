#ifndef LOCALIZATION_MAPPER_H
#define LOCALIZATION_MAPPER_H



#include <map>
#include <optional>
#include <string>
#include <functional>
#include "ModLoader/ModLoader.h"


class Configuration;

namespace mappers {

typedef struct LocBlock {
	std::string english;
	std::string french;
	std::string german;
	std::string russian;
	std::string simp_chinese;
	std::string spanish;
	void modifyForEveryLanguage(const LocBlock& otherLocBlock, const std::function<void(std::string&, const std::string&)>& modifyingFunction);
} LocBlock;

class LocalizationMapper {
public:
	LocalizationMapper() = default;
	void scrapeLocalizations(const Configuration& theConfiguration, const Mods& mods);

	[[nodiscard]] std::optional<LocBlock> getLocBlockForKey(const std::string& key) const;
	void addLocalization(const std::string& key, const LocBlock& locBlock) { localizations[key] = locBlock; }

private:
	void scrapeLanguage(const std::string& language, const std::string& path);
	void scrapeStream(std::istream& theStream, const std::string& language);

	std::map<std::string, LocBlock> localizations;
};

} // namespace mappers



#endif // LOCALIZATION_MAPPER_H