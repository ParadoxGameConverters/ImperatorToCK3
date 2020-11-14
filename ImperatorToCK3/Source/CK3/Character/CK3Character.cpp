#include "CK3Character.h"
#include "../../Imperator/Characters/Character.h"
#include "../../Mappers/CultureMapper/CultureMapper.h"
#include "../../Mappers/ReligionMapper/ReligionMapper.h"
#include "../../Mappers/TraitMapper/TraitMapper.h"
#include "../../Mappers/NicknameMapper/NicknameMapper.h"
#include "Log.h"


void CK3::Character::initializeFromImperator(
	std::shared_ptr<Imperator::Character> impCharacter,
	const mappers::ReligionMapper& religionMapper,
	const mappers::CultureMapper& cultureMapper,
	const mappers::TraitMapper& traitMapper,
	const mappers::NicknameMapper& nicknameMapper,
	const mappers::LocalizationMapper& localizationMapper,
	const bool ConvertBirthAndDeathDates = true,
	const date DateOnConversion = date(867, 1, 1))
{
	imperatorCharacter = std::move(impCharacter);
	ID = "imperator" + std::to_string(imperatorCharacter->getID());
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
	}

	if (!imperatorCharacter->getNickname().empty())
	{
		auto nicknameMatch = nicknameMapper.getCK3NicknameForImperatorNickname(imperatorCharacter->getNickname());
		if (nicknameMatch) nickname = *nicknameMatch;
	}
	
	birthDate = imperatorCharacter->getBirthDate();
	deathDate = imperatorCharacter->getDeathDate();
	if (!ConvertBirthAndDeathDates)  //if option to convert character age is chosen
	{
		birthDate.addYears(static_cast<int>(date(867, 1, 1).diffInYears(DateOnConversion)));
		if (deathDate)
		{
			deathDate->addYears(static_cast<int>(date(867, 1, 1).diffInYears(DateOnConversion)));
		}
	}
}

