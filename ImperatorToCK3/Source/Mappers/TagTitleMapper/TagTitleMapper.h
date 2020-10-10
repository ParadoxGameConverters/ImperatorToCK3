#ifndef TAG_TITLE_MAPPER_H
#define TAG_TITLE_MAPPER_H

#include "Parser.h"

namespace ImperatorWorld {
	enum class countryRankEnum;
}

namespace mappers
{
class TagTitleMapper
{
  public:
	std::optional<std::string> getTitleForTag(const std::string& impTag, ImperatorWorld::countryRankEnum countryRank) const;

  private:
	[[nodiscard]] std::string generateNewTitle(const std::string& impTag, ImperatorWorld::countryRankEnum countryRank) const;

	std::string generatedCK3TitlePrefix = "IMPTOCK3_";
};
} // namespace mappers

#endif // TAG_TITLE_MAPPER_H