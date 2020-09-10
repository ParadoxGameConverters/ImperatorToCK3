#include "TagTitleMapper.h"
#include "Log.h"
#include "ParserHelpers.h"


void mappers::TagTitleMapper::registerTitle(const std::string& impTag, const std::string& ck3Title)
{
	registeredTagTitles.insert(std::pair(impTag, ck3Title));
	usedTitles.insert(ck3Title);
}


std::optional<std::string> mappers::TagTitleMapper::getTitleForTag(const std::string& impTag)
{
	// the only case where we fail is on invalid invocation. Otherwise, failure is
	// not an option!
	if (impTag.empty())
		return std::nullopt;

	// Generate a new tag
	auto generatedTitle = generateNewTitle(impTag);
	registerTitle(impTag, generatedTitle);
	return generatedTitle;
}

std::string mappers::TagTitleMapper::generateNewTitle(const std::string& impTag) const
{
	return generatedCK3TitlePrefix + impTag;
}
