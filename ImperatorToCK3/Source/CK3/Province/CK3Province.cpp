#include "CK3Province.h"
#include "CK3/Titles/Title.h"
#include "Imperator/Provinces/Province.h"
#include "Imperator/Countries/Country.h"
#include "Mappers/CultureMapper/CultureMapper.h"
#include "Mappers/ReligionMapper/ReligionMapper.h"
//#include "Imperator/Characters/Character.h"
#include "Log.h"



using CK3::Province;
using std::shared_ptr;
using mappers::CultureMapper;
using mappers::ReligionMapper;



Province::Province(const unsigned long long id, std::istream& theStream) : ID(id), details(theStream) {} // Load from a country file, if one exists. Otherwise rely on defaults.


Province::Province(const unsigned long long id, const Province& otherProv) : ID(id), baseProvinceID(otherProv.ID), details{otherProv.details} {}


void Province::initializeFromImperator(const shared_ptr<Imperator::Province>& impProvince, const CultureMapper& cultureMapper, const ReligionMapper& religionMapper) {
	imperatorProvince = impProvince;

	// If we're initializing this from Imperator provinces, then having an owner or being a wasteland/sea is not a given -
	// there are uncolonized provinces in Imperator, also uninhabitables have culture and religion.

	if (const auto& impOwnerCountry = impProvince->getOwner().second) {
		ownerTitle = impOwnerCountry->getCK3Title(); // linking to our holder's title
	}

	// Religion first
	setReligionFromImperator(religionMapper);

	// Then culture
	setCultureFromImperator(cultureMapper);

	// Holding type
	setHoldingFromImperator();

	details.buildings.clear();
}


void Province::setReligionFromImperator(const ReligionMapper& religionMapper) {
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
	if (!religionSet && !titleCountry.second->getReligion().empty()) {
		details.religion = titleCountry.second->getReligion();
		religionSet = true;
	}*/
	if (!religionSet) {
		//Use default CK3 religion.
		Log(LogLevel::Debug) << "Couldn't determine religion for province " << ID << " with source religion " << imperatorProvince->getReligion()
							 << ", using vanilla religion";
	}
}


void Province::setCultureFromImperator(const CultureMapper& cultureMapper) {
	auto cultureSet = false;
	// do we even have a base culture?
	if (!imperatorProvince->getCulture().empty()) {
		std::string ownerTitleName;
		if (ownerTitle)
			ownerTitleName = ownerTitle->getName();
		auto cultureMatch = cultureMapper.match(imperatorProvince->getCulture(), details.religion, ID, imperatorProvince->getID(), ownerTitleName);
		if (cultureMatch) {
			details.culture = *cultureMatch;
			cultureSet = true;
		}
	}
	/*
	// Attempt to use primary culture of country. #TODO(#34): use country primary culture as fallback
	if (!cultureSet && !titleCountry.second->getCulture().empty()) {
		details.culture = titleCountry.second->getPrimaryCulture();
		cultureSet = true;
	}*/
	if (!cultureSet) {
		//Use default CK3 culture.
		Log(LogLevel::Debug) << "Couldn't determine culture for province " << ID << " with source culture " << imperatorProvince->getCulture() << ", using vanilla culture";
	}
}

void Province::setHoldingFromImperator() {
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
