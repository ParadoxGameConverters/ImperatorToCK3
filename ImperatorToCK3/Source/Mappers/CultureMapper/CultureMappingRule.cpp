#include "CultureMappingRule.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

mappers::CultureMappingRule::CultureMappingRule(std::istream& theStream)
{
	registerKeyword("ck3", [this](std::istream& theStream) {
		destinationCulture = commonItems::getString(theStream);
	});
	registerKeyword("religion", [this](std::istream& theStream) {
		religions.insert(commonItems::getString(theStream));
	});
	registerKeyword("owner", [this](std::istream& theStream) {
		owners.insert(commonItems::getString(theStream));
	});
	registerKeyword("province", [this](std::istream& theStream) {
		const auto provinceStr = commonItems::getString(theStream);
		try
		{
			provinces.insert(stoull(provinceStr));
		}
		catch (std::exception&)
		{
			Log(LogLevel::Warning) << "Invalid province ID in culture mapper: " << provinceStr;
		}
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		cultures.insert(commonItems::getString(theStream));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}

std::optional<std::string> mappers::CultureMappingRule::cultureMatch(const std::string& impCulture,
	 const std::string& CK3religion,
	 const unsigned long long CK3Province,
	 const std::string& CK3ownerTitle) const
{
	// We need at least a viable CK3culture.
	if (impCulture.empty())
		return std::nullopt;

	if (!cultures.contains(impCulture))
		return std::nullopt;

	if (!owners.empty())
		if (CK3ownerTitle.empty() || !owners.contains(CK3ownerTitle))
			return std::nullopt;

	if (!religions.empty())
	{
		if (CK3religion.empty() || !religions.contains(CK3religion)) // (CK3 religion empty) or (CK3 religion not empty but not found in religions)
			return std::nullopt;
	}

	// This is a straight province check
	if (CK3Province && !provinces.empty())
		if (!provinces.contains(CK3Province))
			return std::nullopt;

	return destinationCulture;
}

std::optional<std::string> mappers::CultureMappingRule::cultureNonReligiousMatch(const std::string& impCulture,
	const std::string& CK3religion,
	const unsigned long long CK3Province,
	const std::string& CK3ownerTitle) const
{
	// This is a non religious match. We need a mapping without any religion, so if the
	// mapping rule has any religious qualifiers it needs to fail.
	if (!religions.empty())
		return std::nullopt;

	// Otherwise, as usual.
	return cultureMatch(impCulture, CK3religion, CK3Province, CK3ownerTitle);
}
