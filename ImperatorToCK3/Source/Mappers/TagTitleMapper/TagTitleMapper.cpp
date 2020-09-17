#include "TagTitleMapper.h"
#include "Log.h"
#include "ParserHelpers.h"


std::optional<std::string> mappers::TagTitleMapper::getTitleForTag(const std::string& impTag)
{
	// the only case where we fail is on invalid invocation. Otherwise, failure is
	// not an option!
	if (impTag.empty())
		return std::nullopt;

	// Generate a new tag
	auto generatedTitle = generateNewTitle(impTag);
	return generatedTitle;
}

std::string mappers::TagTitleMapper::generateNewTitle(const std::string& impTag) const
{
	return generatedCK3TitlePrefix + impTag;
}
