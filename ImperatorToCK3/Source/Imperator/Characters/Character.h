#ifndef IMPERATOR_CHARACTER_H
#define IMPERATOR_CHARACTER_H

#include "Date.h"
#include "Parser.h"
#include <optional>
#include "PortraitData.h"
#include "../Genes/GenesDB.h"

namespace ImperatorWorld
{
class Family;

typedef struct AttributesStruct
{
	int martial = 0;
	int finesse = 0;
	int charisma = 0;
	int zeal = 0;
} AttributesStruct;

class Character: commonItems::parser
{
  public:
	Character(std::istream& theStream, int chrID, const GenesDB& genesDB, const date& _endDate);

	[[nodiscard]] const std::string& getCulture() const;
	[[nodiscard]] const std::string& getReligion() const { return religion; }
	[[nodiscard]] const auto& getName() const { return name; }
	[[nodiscard]] const auto& getBirthDate() const { return birthDate; }
	[[nodiscard]] const auto& getDeathDate() const { return deathDate; }
	[[nodiscard]] const auto& getSpouses() const { return spouses; }
	[[nodiscard]] const auto& getChildren() const { return children; }
	[[nodiscard]] const auto& getMother() const { return mother; }
	[[nodiscard]] const auto& getFather() const { return father; }
	[[nodiscard]] const auto& getFamily() const { return family; }
	[[nodiscard]] const auto& getTraits() const { return traits; }
	[[nodiscard]] const auto& getAttributes() const { return attributes; }
	[[nodiscard]] const auto& getAge() const { return age; }

	[[nodiscard]] const auto& getDNA() const { return dna; }
	[[nodiscard]] const auto& getPortraitData() const { return portraitData; }
	[[nodiscard]] std::string getAgeSex() const;

	[[nodiscard]] auto isFemale() const { return female; }
	[[nodiscard]] auto getWealth() const { return wealth; }
	[[nodiscard]] auto getID() const { return charID; }

	void setFamily(std::shared_ptr<Family> theFamily) { family.second = std::move(theFamily); }
	void setSpouses(const std::map<int, std::shared_ptr<Character>>& newSpouses) { spouses = newSpouses; }
	void setTraits(const std::vector<std::string>& theTraits) { traits = theTraits; }
	void setMother(const std::pair<int, std::shared_ptr<Character>>& theMother) { mother = theMother; }
	void setFather(const std::pair<int, std::shared_ptr<Character>>& theFather) { father = theFather; }
	void registerChild(const std::pair<int, std::shared_ptr<Character>>& theChild) { children.insert(theChild); }
	void addYears(const int years) { birthDate.subtractYears(years); }

  private:
	void registerKeys();

	int charID = 0;
	bool female = false;
	double wealth = 0;
	std::string culture;
	std::string religion;
	std::string name;
	AttributesStruct attributes;
	date birthDate = date("1.1.1");
	date deathDate = date("1.1.1");
	unsigned int age = 0;
	
	std::optional<std::string> dna;
	std::optional<CharacterPortraitData> portraitData;
	GenesDB genes;
	date endDate;

	std::pair<int, std::shared_ptr<Family>> family;
	std::pair<int, std::shared_ptr<Character>> mother;
	std::pair<int, std::shared_ptr<Character>> father;
	std::map<int, std::shared_ptr<Character>> children;
	std::map<int, std::shared_ptr<Character>> spouses;
	std::vector<std::string> traits;
};
} // namespace ImperatorWorld

#endif // IMPERATOR_CHARACTER_H
