#ifndef TAG_TITLE_MAPPER_H
#define TAG_TITLE_MAPPER_H

#include "Parser.h"

namespace mappers
{
class TagTitleMapper
{
  public:
	std::optional<std::string> getTitleForTag(const std::string& impTag);

  private:
	[[nodiscard]] std::string generateNewTitle(const std::string& impTag) const;

	std::string generatedCK3TitlePrefix = "e_IMPTOCK3_";
};
} // namespace mappers

#endif // TAG_TITLE_MAPPER_H