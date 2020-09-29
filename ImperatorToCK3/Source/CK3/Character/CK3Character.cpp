#include "CK3Character.h"
#include "../../Imperator/Characters/Character.h"
#include "../../Mappers/CultureMapper/CultureMapper.h"
#include "../../Mappers/ReligionMapper/ReligionMapper.h"
#include "Log.h"


void CK3::Character::initializeFromImperator(
	std::shared_ptr<ImperatorWorld::Character> impCharacter,
	const mappers::ReligionMapper& religionMapper,
	const mappers::CultureMapper& cultureMapper,
	const mappers::LocalizationMapper& localizationMapper)
{
	ID = impCharacter->getID();
	name = impCharacter->getName();
	female = impCharacter->isFemale();
	auto match = religionMapper.getCK3ReligionForImperatorReligion(impCharacter->getReligion());
	if (match) religion = *match;
	match = cultureMapper.cultureMatch(impCharacter->getCulture(), religion, impCharacter->getProvince(), "");
	if (match) culture = *match;

	auto impName = impCharacter->getName();
	if (!impName.empty())
	{
		auto impNameLoc = localizationMapper.getLocBlockForKey(impName);
		if (impNameLoc)
		{
			localizations.insert(std::pair(impName, *impNameLoc));
		}
		else
		{
			localizations.insert(std::pair(impName, mappers::LocBlock{impName,impName,impName,impName,impName}));
		}
	}
}

