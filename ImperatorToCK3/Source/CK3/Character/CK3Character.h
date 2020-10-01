#ifndef CK3_CHARACTER_H
#define CK3_CHARACTER_H

#include "../../Mappers/LocalizationMapper/LocalizationMapper.h"
#include "../../Imperator/Characters/Character.h"
#include <memory>
#include <set>
#include <string>


namespace mappers
{
	class CultureMapper;
	class ReligionMapper;
	class TraitMapper;
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
		const mappers::TraitMapper& traitMapper,
		const mappers::LocalizationMapper& localizationMapper,
		bool ConvertBirthAndDeathDates,
		date DateOnConversion);


	void addSpouse(const std::pair<std::string, std::shared_ptr<Character>>& newSpouse) { spouses.insert(newSpouse); }
	void setMother(const std::pair<std::string, std::shared_ptr<Character>>& theMother) { mother = theMother; }
	void setFather(const std::pair<std::string, std::shared_ptr<Character>>& theFather) { father = theFather; }
	void addChild(const std::pair<std::string, std::shared_ptr<Character>>& theChild) { children.insert(theChild); }

	friend std::ostream& operator<<(std::ostream& output, const Character& character);

	std::string ID = "0";
	bool female = false;
	std::string culture;
	std::string religion;
	std::string name;
	unsigned int age = 0; // used when option to convert character age is chosen

	date birthDate = date("840.1.1"); // temporary
	std::optional<date> deathDate;

	std::set<std::string> traits;
	std::map<std::string, mappers::LocBlock> localizations;
	
	std::shared_ptr<ImperatorWorld::Character> imperatorCharacter;

  private:
	std::pair<std::string, std::shared_ptr<Character>> mother;
	std::pair<std::string, std::shared_ptr<Character>> father;
	std::map<std::string, std::shared_ptr<Character>> children;
	std::map<std::string, std::shared_ptr<Character>> spouses;

};
} // namespace CK3

#endif // CK3_CHARACTER_H
