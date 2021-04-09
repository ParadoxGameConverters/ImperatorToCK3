#ifndef CK3_CHARACTER_H
#define CK3_CHARACTER_H



#include "Mappers/LocalizationMapper/LocalizationMapper.h"
#include "Date.h"
#include <memory>
#include <set>
#include <string>



namespace mappers {
class CultureMapper;
class DeathReasonMapper;
class NicknameMapper;
class ProvinceMapper;
class ReligionMapper;
class TraitMapper;
} // namespace mappers

namespace Imperator {
class Character;
}

namespace CK3 {

class Dynasty;
class Character {
  public:
	Character() = default;
	void initializeFromImperator(
		std::shared_ptr<Imperator::Character> impCharacter,
		const mappers::ReligionMapper& religionMapper,
		const mappers::CultureMapper& cultureMapper,
		const mappers::TraitMapper& traitMapper,
		const mappers::NicknameMapper& nicknameMapper,
		const mappers::LocalizationMapper& localizationMapper,
		const mappers::ProvinceMapper& provinceMapper,
		const mappers::DeathReasonMapper& deathReasonMapper,
		bool ConvertBirthAndDeathDates,
		date DateOnConversion);


	void addSpouse(const std::pair<std::string, std::shared_ptr<Character>>& newSpouse) { spouses.insert(newSpouse); }
	void setMother(const std::pair<std::string, std::shared_ptr<Character>>& theMother) { mother = theMother; }
	void setFather(const std::pair<std::string, std::shared_ptr<Character>>& theFather) { father = theFather; }
	void addChild(const std::pair<std::string, std::shared_ptr<Character>>& theChild) { children.insert(theChild); }
	void setDynastyID(const std::string& dynID) { dynastyID = dynID; }

	friend std::ostream& operator<<(std::ostream& output, const Character& character);

	std::string ID = "0";
	bool female = false;
	std::string culture;
	std::string religion;
	std::string name;
	std::string nickname;
	unsigned int age = 0; // used when option to convert character age is chosen

	date birthDate = date("1.1.1");
	std::optional<date> deathDate;
	std::optional<std::string> deathReason;

	std::set<std::string> traits;
	std::map<std::string, mappers::LocBlock> localizations;
	
	std::shared_ptr<Imperator::Character> imperatorCharacter;

  private:
	std::pair<std::string, std::shared_ptr<Character>> mother;
	std::pair<std::string, std::shared_ptr<Character>> father;
	std::map<std::string, std::shared_ptr<Character>> children;
	std::map<std::string, std::shared_ptr<Character>> spouses;

	std::optional<std::string> dynastyID; // not always set
};

} // namespace CK3



#endif // CK3_CHARACTER_H
