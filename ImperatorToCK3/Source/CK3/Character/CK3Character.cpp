#include "CK3Character.h"
#include "../../Imperator/Characters/Character.h"
#include "../../Mappers/CultureMapper/CultureMapper.h"
#include "../../Mappers/ReligionMapper/ReligionMapper.h"
#include "../../Mappers/TraitMapper/TraitMapper.h"
#include "Log.h"


void CK3::Character::initializeFromImperator(
	std::shared_ptr<ImperatorWorld::Character> impCharacter,
	const mappers::ReligionMapper& religionMapper,
	const mappers::CultureMapper& cultureMapper,
	const mappers::TraitMapper& traitMapper,
	const mappers::LocalizationMapper& localizationMapper,
	const bool ConvertBirthAndDeathDates = true,
	const date DateOnConversion = date(867, 1, 1))
{
	imperatorCharacter = std::move(impCharacter);
	ID = std::to_string(imperatorCharacter->getID());
	name = imperatorCharacter->getName();
	female = imperatorCharacter->isFemale();
	age = imperatorCharacter->getAge();
	
	auto match = religionMapper.getCK3ReligionForImperatorReligion(imperatorCharacter->getReligion());
	if (match) religion = *match;
	match = cultureMapper.cultureMatch(imperatorCharacter->getCulture(), religion, imperatorCharacter->getProvince(), "");
	if (match) culture = *match;

	if (!name.empty())
	{
		auto impNameLoc = localizationMapper.getLocBlockForKey(name);
		if (impNameLoc)
		{
			localizations.insert(std::pair(name, *impNameLoc));
		}
		else // use unlocalized name as displayed name as fallback
		{
			localizations.insert(std::pair(name, mappers::LocBlock{ name,name,name,name,name }));
		}
	}

	for (const auto& impTrait : imperatorCharacter->getTraits())
	{
		auto traitMatch = traitMapper.getCK3TraitForImperatorTrait(impTrait);
		if (traitMatch) traits.insert(*traitMatch);
		//else LOG(LogLevel::Warning) << ID << ": No mapping found for Imperator trait " << impTrait << ", dropping."; // too many are missing ATM, enabling this would flood the log
	}

	
	birthDate = imperatorCharacter->getBirthDate();
	deathDate = imperatorCharacter->getDeathDate();
	if (!ConvertBirthAndDeathDates)  //if option to convert character age is chosen
	{
		birthDate.subtractYears(- static_cast<int>(date(867, 1, 1).diffInYears(DateOnConversion)));
		if (imperatorCharacter->getDeathDate())
		{
			deathDate = birthDate;
			deathDate->subtractYears(-static_cast<int>(age));
		}
	}
}

