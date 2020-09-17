#ifndef TAG_TITLE_MAPPER_H
#define TAG_TITLE_MAPPER_H

#include "Parser.h"
#include <set>

namespace mappers
{
class TagTitleMapper
{
  public:
	void registerTitle(const std::string& impTag, const std::string& ck3Title);

	std::optional<std::string> getTitleForTag(const std::string& impTag);

	[[nodiscard]] const auto& getRegisteredTitleTags() const { return registeredTagTitles; } // used for testing

  private:
	[[nodiscard]] std::string generateNewTitle(const std::string& impTag) const;

	std::map<std::string, std::string> registeredTagTitles; // We store already mapped countries here.
	std::set<std::string> usedTitles;

	std::string generatedCK3TitlePrefix = "e_IMPTOCK3_";
};
} // namespace mappers

#endif // TAG_TITLE_MAPPER_H