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
	ID = std::to_string(impCharacter->getID());
	name = impCharacter->getName();
	female = impCharacter->isFemale();
	auto match = religionMapper.getCK3ReligionForImperatorReligion(impCharacter->getReligion());
	if (match) religion = *match;
	match = cultureMapper.cultureMatch(impCharacter->getCulture(), religion, impCharacter->getProvince(), "");
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
}

