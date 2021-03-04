#include "CK3Province.h"
#include "Imperator/Provinces/Province.h"
//#include "Imperator/Countries/Country.h"
#include "Mappers/CultureMapper/CultureMapper.h"
#include "Mappers/ReligionMapper/ReligionMapper.h"
//#include "Title/Title.h"
//#include "Imperator/Characters/Character.h"



CK3::Province::Province(const unsigned long long id, std::istream& theStream) : ID(id), details(theStream) {} // Load from a country file, if one exists. Otherwise rely on defaults.

CK3::Province::Province(const unsigned long long id, const Province& otherProv) : ID(id), details{ otherProv.details } {}

void CK3::Province::initializeFromImperator(const std::shared_ptr<Imperator::Province>& origProvince,
                                            const mappers::CultureMapper& cultureMapper,
                                            const mappers::ReligionMapper& religionMapper)
{
	imperatorProvince = origProvince;
	
	// If we're initializing this from Imperator provinces, then having an owner or being a wasteland/sea is not a given -
	// there are uncolonized provinces in Imperator, also uninhabitables have culture and religion.

	/*
	titleCountry = srcProvince->getOwner().second->getCK3Title(); // linking to our holder*/

	// Religion first
	setReligion(religionMapper);
	
	// Then culture
	setCulture(cultureMapper);

	// Holding type
	setHolding();
}


void CK3::Province::setReligion(const mappers::ReligionMapper& religionMapper) {
	auto religionSet = false;
	if (!imperatorProvince->getReligion().empty()) {
		auto religionMatch = religionMapper.match(imperatorProvince->getReligion(), ID, imperatorProvince->getID());
		if (religionMatch) {
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
	if (!religionSet) {
		//Use default CK3 religion.
	}
}


void CK3::Province::setCulture(const mappers::CultureMapper& cultureMapper)
{
	auto cultureSet = false;
	// do we even have a base culture?
	if (!imperatorProvince->getCulture().empty()) {
		auto cultureMatch = cultureMapper.match(imperatorProvince->getCulture(), details.religion, ID, imperatorProvince->getID(), titleCountry.first);
		if (cultureMatch) {
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
	if (!cultureSet) {
		//Use default CK3 culture.
	}
}

void CK3::Province::setHolding() {
	switch (imperatorProvince->getProvinceRank()) {
	case Imperator::ProvinceRank::city_metropolis:
		details.holding = "city_holding";
		break;
	case Imperator::ProvinceRank::city:
		if (imperatorProvince->hasFort())
			details.holding = "castle_holding";
		else
			details.holding = "city_holding";
		break;
	case Imperator::ProvinceRank::settlement:
		if (imperatorProvince->isHolySite())
			details.holding = "church_holding";
		else if (imperatorProvince->hasFort())
			details.holding = "castle_holding";
		else
			details.holding = "tribal_holding";
		break;
	}
}
