#include "CK3Province.h"
#include "../../Imperator/Provinces/Province.h"
//#include "../../Imperator/Countries/Country.h"
#include "../../Mappers/CultureMapper/CultureMapper.h"
#include "../../Mappers/ReligionMapper/ReligionMapper.h"
//#include "../Title/Title.h"
//#include "../../Imperator/Characters/Character.h"

CK3::Province::Province(int id, std::istream& theStream) : provID(id), details(theStream) {} // Load from a country file, if one exists. Otherwise rely on defaults.


void CK3::Province::updateWith(const std::string& filePath)
{
	// We're doing this for special reason and from a specific source.
	details.updateWith(filePath);
}

void CK3::Province::initializeFromImperator(std::shared_ptr<ImperatorWorld::Province> origProvince,
	 const mappers::CultureMapper& cultureMapper,
	 const mappers::ReligionMapper& religionMapper)
{
	srcProvince = std::move(origProvince);
	
	// If we're initializing this from Imperator provinces, then having an owner or being a wasteland/sea is not a given -
	// there are uncolonized provinces in Imperator, also uninhabitables have culture and religion.

	/*
	titleCountry = srcProvince->getOwner().second->getCK3Title(); // linking to our holder
	details.owner = titleCountry.first;
	details.controller = titleCountry.first; */

	// Religion first
	setReligion(religionMapper);
	
	// Then culture
	setCulture(cultureMapper);
}

void CK3::Province::setReligion(const mappers::ReligionMapper& religionMapper)
{
	auto religionSet = false;
	if (!srcProvince->getReligion().empty())
	{
		auto religionMatch = religionMapper.getCK3ReligionForImperatorReligion(srcProvince->getReligion());
		if (religionMatch)
		{
			details.religion = *religionMatch;
			religionSet = true;
		}
	}
	/*
	// Attempt to use religion of country. #TODO(#34): use country religion as fallback
	if (!religionSet && !titleCountry.second->getReligion().empty())
	{
		details.religion = titleCountry.second->getReligion();
		religionSet = true;
	}*/
	if (!religionSet)
	{
		//Use default CK3 religion.
	}
}


void CK3::Province::setCulture(const mappers::CultureMapper& cultureMapper)
{
	auto cultureSet = false;
	// do we even have a base culture?
	if (!srcProvince->getCulture().empty())
	{
		auto cultureMatch = cultureMapper.cultureMatch(srcProvince->getCulture(), details.religion, provID, titleCountry.first);
		if (cultureMatch)
		{
			details.culture = *cultureMatch;
			cultureSet = true;
		}
	}
	/*
	// Attempt to use primary culture of country. #TODO(#34): use country primary culture as fallback
	if (!cultureSet && !titleCountry.second->getPrimaryCulture().empty())
	{
		details.culture = titleCountry.second->getPrimaryCulture();
		cultureSet = true;
	}
	*/
	if (!cultureSet)
	{
		//Use default CK3 culture.
	}
}
