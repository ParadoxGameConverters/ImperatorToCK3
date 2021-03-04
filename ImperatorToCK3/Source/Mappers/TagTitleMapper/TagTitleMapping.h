#ifndef TAG_TITLE_MAPPING_H
#define TAG_TITLE_MAPPING_H



#include "Parser.h"
#include <set>



namespace mappers {

class TagTitleMapping: commonItems::parser {
  public:
	TagTitleMapping() = default;
	explicit TagTitleMapping(std::istream& theStream);

	[[nodiscard]] std::optional<std::string> tagRankMatch(const std::string& impTag, const std::string& rank) const;

  private:
	void registerKeys();

	std::string ck3Title;
	std::string imperatorTag;
	std::set<std::string> ranks;
};

} // namespace mappers



#endif // TAG_TITLE_MAPPING_H