#include "gtest/gtest.h"
#include "Imperator/Characters/Character.h"
#include "Imperator/Characters/CharacterFactory.h"
#include "Imperator/Families/Family.h"
#include "Imperator/Families/FamilyFactory.h"
#include "Imperator/Genes/GenesDB.h"
#include "Date.h"
#include <sstream>



TEST(ImperatorWorld_CharacterTests, IDCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(42, theCharacter.getID());
}

TEST(ImperatorWorld_CharacterTests, cultureCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tculture=\"paradoxian\"";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ("paradoxian", theCharacter.getCulture());
}

TEST(ImperatorWorld_CharacterTests, cultureDefaultsToBlank) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_TRUE(theCharacter.getCulture().empty());
}


TEST(ImperatorWorld_CharacterTests, religionCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\treligion=\"paradoxian\"";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ("paradoxian", theCharacter.getReligion());
}

TEST(ImperatorWorld_CharacterTests, religionDefaultsToBlank) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_TRUE(theCharacter.getReligion().empty());
}

TEST(ImperatorWorld_CharacterTests, sexCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tfemale=yes";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_TRUE(theCharacter.isFemale());
}

TEST(ImperatorWorld_CharacterTests, sexDefaultsToMale) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_FALSE(theCharacter.isFemale());
}

TEST(ImperatorWorld_CharacterTests, traitsCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	const std::vector<std::string> traitsVector{ "lustful", "submissive", "greedy" };

	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\ttraits = { \"lustful\" \"submissive\" \"greedy\" }";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(traitsVector, theCharacter.getTraits());
}

TEST(ImperatorWorld_CharacterTests, traitsDefaultToEmpty) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_TRUE(theCharacter.getTraits().empty());
}

TEST(ImperatorWorld_CharacterTests, birthDateCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tbirth_date=408.6.28"; // will be converted to AD on loading
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(date("-346.6.28"), theCharacter.getBirthDate());
}

TEST(ImperatorWorld_CharacterTests, birthDateDefaultsTo1_1_1) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(date("1.1.1"), theCharacter.getBirthDate());
}

TEST(ImperatorWorld_CharacterTests, deathDateCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tdeath_date=408.6.28"; // will be converted to AD on loading
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(date("-346.6.28"), theCharacter.getDeathDate());
}

TEST(ImperatorWorld_CharacterTests, deathDateDefaultsToNullopt) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_FALSE(theCharacter.getDeathDate());
}

TEST(ImperatorWorld_CharacterTests, spousesCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tspouse= { 69 420 } ";
	input << "}";

	std::stringstream spouse69input;
	spouse69input << "=\n";
	spouse69input << "{\n";
	spouse69input << "}";

	std::stringstream spouse420input;
	spouse420input << "=\n";
	spouse420input << "{\n";
	spouse420input << "}";

	auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);
	std::map<unsigned long long, std::shared_ptr<Imperator::Character>> spousesMap;
	spousesMap.emplace(69, Imperator::Character::Factory().getCharacter(spouse69input, "69", genesDB));
	spousesMap.emplace(420, Imperator::Character::Factory().getCharacter(spouse420input, "420", genesDB));
	theCharacter.setSpouses(spousesMap);

	ASSERT_FALSE(theCharacter.getSpouses().empty());
	ASSERT_EQ(69, theCharacter.getSpouses().find(69)->first);
	ASSERT_EQ(69, theCharacter.getSpouses().find(69)->second->getID());
	ASSERT_EQ(420, theCharacter.getSpouses().find(420)->first);
	ASSERT_EQ(420, theCharacter.getSpouses().find(420)->second->getID());
}

TEST(ImperatorWorld_CharacterTests, spousesDefaultToEmpty) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_TRUE(theCharacter.getSpouses().empty());
}

TEST(ImperatorWorld_CharacterTests, childrenCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tchildren = { 69 420 } ";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_FALSE(theCharacter.getChildren().empty());
	ASSERT_EQ(69, theCharacter.getChildren().find(69)->first);
	ASSERT_EQ(420, theCharacter.getChildren().find(420)->first);
}

TEST(ImperatorWorld_CharacterTests, childrenDefaultToEmpty) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_TRUE(theCharacter.getChildren().empty());
}

TEST(ImperatorWorld_CharacterTests, motherCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tmother=123";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(123, theCharacter.getMother().first);
}

TEST(ImperatorWorld_CharacterTests, motherDefaultsToZero) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(0, theCharacter.getMother().first);
}

TEST(ImperatorWorld_CharacterTests, fatherCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tfather=123";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(123, theCharacter.getFather().first);
}

TEST(ImperatorWorld_CharacterTests, fatherDefaultsToZero) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(0, theCharacter.getFather().first);
}

TEST(ImperatorWorld_CharacterTests, familyCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tfamily=123";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(123, theCharacter.getFamily().first);
}

TEST(ImperatorWorld_CharacterTests, familyDefaultsToZero) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(0, theCharacter.getFamily().first);
}

TEST(ImperatorWorld_CharacterTests, wealthCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "wealth=\"420.5\"";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_DOUBLE_EQ(420.5, theCharacter.getWealth());
}

TEST(ImperatorWorld_CharacterTests, wealthDefaultsToZero) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(0, theCharacter.getWealth());
}

TEST(ImperatorWorld_CharacterTests, nameCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "first_name_loc = {\n";
	input << "name=\"Biggus Dickus\"\n";
	input << "}\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ("Biggus Dickus", theCharacter.getName());
}

TEST(ImperatorWorld_CharacterTests, nameDefaultsToBlank) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_TRUE(theCharacter.getName().empty());
}

TEST(ImperatorWorld_CharacterTests, attributesDefaultToZero) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(0, theCharacter.getAttributes().martial);
	ASSERT_EQ(0, theCharacter.getAttributes().finesse);
	ASSERT_EQ(0, theCharacter.getAttributes().charisma);
	ASSERT_EQ(0, theCharacter.getAttributes().zeal);
}

TEST(ImperatorWorld_CharacterTests, attributesCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tattributes={ martial=1 finesse=2 charisma=3 zeal=4 }";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(1, theCharacter.getAttributes().martial);
	ASSERT_EQ(2, theCharacter.getAttributes().finesse);
	ASSERT_EQ(3, theCharacter.getAttributes().charisma);
	ASSERT_EQ(4, theCharacter.getAttributes().zeal);
}

TEST(ImperatorWorld_CharacterTests, cultureCanBeInheritedFromFamily) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream familyInput;
	familyInput << "=\n";
	familyInput << "{\n";
	familyInput << "\tculture=paradoxian";
	familyInput << "}";

	std::stringstream characterInput;
	characterInput << "=\n";
	characterInput << "{\n";
	characterInput << "\tfamily=42";
	characterInput << "}";

	const auto theFamily = *Imperator::Family::Factory().getFamily(familyInput, 42);

	auto theCharacter = *Imperator::Character::Factory().getCharacter(characterInput, "69", genesDB);

	if (theCharacter.getFamily().first == theFamily.getID()) {
		auto familyPointer = std::make_shared<Imperator::Family>(theFamily);
		theCharacter.setFamily(familyPointer);
	}

	ASSERT_EQ("paradoxian", theCharacter.getCulture());
}


TEST(ImperatorWorld_CharacterTests, dnaCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tdna=\"paradoxian\"";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ("paradoxian", theCharacter.getDNA());
}

TEST(ImperatorWorld_CharacterTests, dnaDefaultsToNullopt) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_FALSE(theCharacter.getDNA());
}


TEST(ImperatorWorld_CharacterTests, portraitDataIsNotExtractedFromDnaOfWrongLength) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "={dna=\"AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/==\"}";
	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_FALSE(theCharacter.getPortraitData());
}


TEST(ImperatorWorld_CharacterTests, colorPaletteCoordinatesCanBeExtractedFromDNA) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "={dna=\"AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==\"}";
	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(0, theCharacter.getPortraitData().value().getHairColorPaletteCoordinates().x);
	ASSERT_EQ(0, theCharacter.getPortraitData().value().getHairColorPaletteCoordinates().y);
	ASSERT_EQ(0, theCharacter.getPortraitData().value().getSkinColorPaletteCoordinates().x);
	ASSERT_EQ(0, theCharacter.getPortraitData().value().getSkinColorPaletteCoordinates().y);
	ASSERT_EQ(0, theCharacter.getPortraitData().value().getEyeColorPaletteCoordinates().x);
	ASSERT_EQ(0, theCharacter.getPortraitData().value().getEyeColorPaletteCoordinates().y);
}


TEST(ImperatorWorld_CharacterTests, ageCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tage=56\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(56, theCharacter.getAge());
}

TEST(ImperatorWorld_CharacterTests, ageDefaultsTo0) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(0, theCharacter.getAge());
}


TEST(ImperatorWorld_CharacterTests, getAgeSexReturnsCorrectString) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input, input2, input3, input4;
	input << "=\n";
	input << "{\n";
	input << "\tage=56\n";
	input << "\tfemale=yes\n";
	input << "}";
	
	input2 << "=\n";
	input2 << "{\n";
	input2 << "\tage=56\n";
	input2 << "}";
	
	input3 << "=\n";
	input3 << "{\n";
	input3 << "\tage=8\n";
	input3 << "\tfemale=yes\n";
	input3 << "}";
	
	input4 << "=\n";
	input4 << "{\n";
	input4 << "\tage=8\n";
	input4 << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);
	const auto theCharacter2 = *Imperator::Character::Factory().getCharacter(input2, "43", genesDB);
	const auto theCharacter3 = *Imperator::Character::Factory().getCharacter(input3, "44", genesDB);
	const auto theCharacter4 = *Imperator::Character::Factory().getCharacter(input4, "45", genesDB);

	ASSERT_EQ("female", theCharacter.getAgeSex());
	ASSERT_EQ("male", theCharacter2.getAgeSex());
	ASSERT_EQ("girl", theCharacter3.getAgeSex());
	ASSERT_EQ("boy", theCharacter4.getAgeSex());
}

TEST(ImperatorWorld_CharacterTests, provinceCanBeSet) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tprovince=69";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(69, theCharacter.getProvince());
}

TEST(ImperatorWorld_CharacterTests, provinceDefaultsTo0) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ(0, theCharacter.getProvince());
}

TEST(ImperatorWorld_CharacterTests, AUC0ConvertsTo754BC) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "= { birth_date = 0.1.1 }";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ("-754.1.1", theCharacter.getBirthDate().toString());
}

TEST(ImperatorWorld_CharacterTests, AUC753ConvertsTo1BC) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "= { birth_date = 753.1.1 }";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ("-1.1.1", theCharacter.getBirthDate().toString());
}

TEST(ImperatorWorld_CharacterTests, AUC754ConvertsTo1AD) {
	const auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::stringstream input;
	input << "= { birth_date = 754.1.1 }";

	const auto theCharacter = *Imperator::Character::Factory().getCharacter(input, "42", genesDB);

	ASSERT_EQ("1.1.1", theCharacter.getBirthDate().toString());
}
