#include "TagTitleMapper.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "../../Imperator/Countries/Country.h"


std::optional<std::string> mappers::TagTitleMapper::getTitleForTag(const std::string& impTag, const Imperator::countryRankEnum countryRank, const std::string& localizedTitleName) const
{
	// the only case where we fail is on invalid invocation. Otherwise, failure is
	// not an option!
	if (impTag.empty())
		return std::nullopt;

	// Generate a new tag
	auto generatedTitle = generateNewTitle(impTag, countryRank, localizedTitleName);
	return generatedTitle;
}

std::string mappers::TagTitleMapper::generateNewTitle(const std::string& impTag, const Imperator::countryRankEnum countryRank, const std::string& localizedTitleName) const
{
	std::string ck3Tag;

	if (localizedTitleName.find("Empire") != std::string::npos) ck3Tag += "e_";
	else if (localizedTitleName.find("Kingdom") != std::string::npos) ck3Tag += "k_";
	else switch (countryRank)
	{
	case Imperator::countryRankEnum::migrantHorde: { ck3Tag += "d_"; break; }
	case Imperator::countryRankEnum::cityState: { ck3Tag += "d_"; break; }
	case Imperator::countryRankEnum::localPower: { ck3Tag += "k_"; break; }
	case Imperator::countryRankEnum::regionalPower: { ck3Tag += "k_"; break; }
	case Imperator::countryRankEnum::majorPower: { ck3Tag += "k_"; break; }
	case Imperator::countryRankEnum::greatPower: { ck3Tag += "e_"; break; }
	}
	
	ck3Tag += generatedCK3TitlePrefix;
	ck3Tag += impTag;
	
	return ck3Tag;
}
