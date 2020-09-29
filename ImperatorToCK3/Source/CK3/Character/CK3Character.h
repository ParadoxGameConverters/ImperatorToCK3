#ifndef CK3_CHARACTER_H
#define CK3_CHARACTER_H

#include "../../Mappers/LocalizationMapper/LocalizationMapper.h"
#include "../../Imperator/Characters/Character.h"
#include <memory>
#include <string>


namespace mappers
{
	class CultureMapper;
	class ReligionMapper;
} // namespace mappers

namespace CK3
{
class Character
{
  public:
	Character() = default;
	void initializeFromImperator(
		std::shared_ptr<ImperatorWorld::Character> impCharacter,
		const mappers::ReligionMapper& religionMapper,
		const mappers::CultureMapper& cultureMapper,
		const mappers::LocalizationMapper& localizationMapper);

	friend std::ostream& operator<<(std::ostream& output, const Character& character);

	std::string ID = "0";
	bool female = false;
	std::string culture;
	std::string religion;
	std::string name;

	date birthDate = date("840.1.1"); // temporary
	std::optional<date> deathDate;

	std::map<std::string, mappers::LocBlock> localizations;

  private:

	std::shared_ptr<ImperatorWorld::Character> imperatorCharacter;
};
} // namespace CK3

#endif // CK3_CHARACTER_H
