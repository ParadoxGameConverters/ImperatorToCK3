#include "CK3Character.h"
#include <ranges>
#include "Imperator/Characters/Character.h"
#include "Mappers/CultureMapper/CultureMapper.h"
#include "Mappers/DeathReasonMapper/DeathReasonMapper.h"
#include "Mappers/NicknameMapper/NicknameMapper.h"
#include "Mappers/ProvinceMapper/ProvinceMapper.h"
#include "Mappers/ReligionMapper/ReligionMapper.h"
#include "Mappers/TraitMapper/TraitMapper.h"



void CK3::Character::initializeFromImperator(std::shared_ptr<Imperator::Character> impCharacter,
											 const mappers::ReligionMapper& religionMapper,
											 const mappers::CultureMapper& cultureMapper,
											 const mappers::TraitMapper& traitMapper,
											 const mappers::NicknameMapper& nicknameMapper,
											 const mappers::LocalizationMapper& localizationMapper,
											 const mappers::ProvinceMapper& provinceMapper,	 // used to determine ck3 province for religion mapper
											 const mappers::DeathReasonMapper& deathReasonMapper,
											 const bool ConvertBirthAndDeathDates = true,
											 const date DateOnConversion = date(867, 1, 1)) {
	imperatorCharacter = std::move(impCharacter);
	ID = "imperator" + std::to_string(imperatorCharacter->getID());
	name = imperatorCharacter->getName();
	female = imperatorCharacter->isFemale();
	age = imperatorCharacter->getAge();


	unsigned long long ck3Province;	 // for religion mapper
	// Determine valid (not dropped in province mappings) "source province" to be used by religion mapper. Don't give up without a fight.
	auto impProvForProvinceMapper = imperatorCharacter->getProvince();
	if (provinceMapper.getCK3ProvinceNumbers(impProvForProvinceMapper).empty() && imperatorCharacter->getFather().second)
		impProvForProvinceMapper = imperatorCharacter->getFather().second->getProvince();
	if (provinceMapper.getCK3ProvinceNumbers(impProvForProvinceMapper).empty() && imperatorCharacter->getMother().second)
		impProvForProvinceMapper = imperatorCharacter->getMother().second->getProvince();
	if (provinceMapper.getCK3ProvinceNumbers(impProvForProvinceMapper).empty() && !imperatorCharacter->getSpouses().empty())
		impProvForProvinceMapper = imperatorCharacter->getSpouses().begin()->second->getProvince();

	auto ck3ProvinceNumbers = provinceMapper.getCK3ProvinceNumbers(impProvForProvinceMapper);
	if (ck3ProvinceNumbers.empty())
		ck3Province = 0;
	else
		ck3Province = ck3ProvinceNumbers[0];

	auto match = religionMapper.match(imperatorCharacter->getReligion(), ck3Province, imperatorCharacter->getProvince());
	if (match)
		religion = *match;


	match = cultureMapper.match(imperatorCharacter->getCulture(), religion, ck3Province, imperatorCharacter->getProvince(), "");
	if (match)
		culture = *match;

	if (!name.empty()) {
		auto impNameLoc = localizationMapper.getLocBlockForKey(name);
		if (impNameLoc) {
			localizations.emplace(name, *impNameLoc);
		} else {  // fallback: use unlocalized name as displayed name
			localizations.emplace(name, mappers::LocBlock{name, name, name, name, name, name});
		}
	}

	for (const auto& impTrait : imperatorCharacter->getTraits()) {
		auto traitMatch = traitMapper.getCK3TraitForImperatorTrait(impTrait);
		if (traitMatch)
			traits.emplace(*traitMatch);
	}

	if (!imperatorCharacter->getNickname().empty()) {
		auto nicknameMatch = nicknameMapper.getCK3NicknameForImperatorNickname(imperatorCharacter->getNickname());
		if (nicknameMatch)
			nickname = *nicknameMatch;
	}

	birthDate = imperatorCharacter->getBirthDate();
	deathDate = imperatorCharacter->getDeathDate();
	if (const auto& impDeathReason = imperatorCharacter->getDeathReason(); impDeathReason) {
		deathReason = deathReasonMapper.getCK3ReasonForImperatorReason(*impDeathReason);
	}
	if (!ConvertBirthAndDeathDates) {  // if option to convert character age is chosen
		birthDate.addYears(static_cast<int>(date(867, 1, 1).diffInYears(DateOnConversion)));
		if (deathDate) {
			deathDate->addYears(static_cast<int>(date(867, 1, 1).diffInYears(DateOnConversion)));
		}
	}
}


void CK3::Character::breakAllLinks() {
	if (mother.second) {
		mother.second->removeChild(ID);
	}
	removeMother();
	if (father.second) {
		father.second->removeChild(ID);
	}
	removeFather();
	for (const auto& spousePtr : spouses | std::views::values) {
		spousePtr->removeSpouse(ID);
	}
	spouses.clear();
	if (female) {
		for (const auto& childPtr : children | std::views::values) {
			childPtr->removeMother();
		}
	} else {
		for (const auto& childPtr : children | std::views::values) {
			childPtr->removeFather();
		}
	}
	children.clear();

	imperatorCharacter->registerCK3Character(nullptr);
	imperatorCharacter = nullptr;
}


void CK3::Character::removeSpouse(const std::string& spouseID) {
	spouses.erase(spouseID);
}


void CK3::Character::removeFather() {
	father.first = "0";
	father.second = nullptr;
}


void CK3::Character::removeMother() {
	mother.first = "0";
	mother.second = nullptr;
}


void CK3::Character::removeChild(const std::string& childID) {
	children.erase(childID);
}
