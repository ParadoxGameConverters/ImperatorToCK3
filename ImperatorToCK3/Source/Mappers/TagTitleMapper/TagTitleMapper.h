#ifndef TAG_TITLE_MAPPER_H
#define TAG_TITLE_MAPPER_H



#include "TagTitleMapping.h"
#include "Parser.h"



namespace Imperator {
class Country;
enum class countryRankEnum;
}

namespace mappers {

class TagTitleMapper: commonItems::parser {
  public:
	TagTitleMapper();
	void registerTag(const std::string& impTag, const std::string& ck3Title);

	[[nodiscard]] std::optional<std::string> getTitleForTag(const std::string& impTag, Imperator::countryRankEnum countryRank, const std::string& localizedTitleName);
	[[nodiscard]] std::optional<std::string> getTitleForTag(const std::string& impTag, const Imperator::countryRankEnum countryRank) { return getTitleForTag(impTag, countryRank, ""); }

  private:
	void registerKeys();
	[[nodiscard]] std::string getCK3TitleRank(Imperator::countryRankEnum impRank, const std::string& localizedTitleName) const;
	[[nodiscard]] std::string generateNewTitle(const std::string& impTag, Imperator::countryRankEnum countryRank, const std::string& localizedTitleName) const;

	std::vector<TagTitleMapping> theMappings;
	std::map<std::string, std::string> registeredTagTitles; // We store already mapped countries here.
	std::set<std::string> usedTitles;

	std::string generatedCK3TitlePrefix = "IMPTOCK3_";
};

} // namespace mappers



#endif // TAG_TITLE_MAPPER_H