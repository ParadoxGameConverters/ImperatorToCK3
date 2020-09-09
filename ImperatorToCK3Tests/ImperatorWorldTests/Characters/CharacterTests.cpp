#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Characters/Character.h"
#include "../ImperatorToCK3/Source/Imperator/Families/Family.h"
#include "../commonItems/Date.h"
#include <sstream>

TEST(ImperatorWorld_CharacterTests, IDCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(42, theCharacter.getID());
}

TEST(ImperatorWorld_CharacterTests, cultureCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tculture=\"paradoxian\"";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ("paradoxian", theCharacter.getCulture());
}

TEST(ImperatorWorld_CharacterTests, cultureDefaultsToBlank)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_TRUE(theCharacter.getCulture().empty());
}


TEST(ImperatorWorld_CharacterTests, religionCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\treligion=\"paradoxian\"";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ("paradoxian", theCharacter.getReligion());
}

TEST(ImperatorWorld_CharacterTests, religionDefaultsToBlank)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_TRUE(theCharacter.getReligion().empty());
}

TEST(ImperatorWorld_CharacterTests, sexCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tfemale=yes";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_TRUE(theCharacter.isFemale());
}
TEST(ImperatorWorld_CharacterTests, sexDefaultsToMale)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_FALSE(theCharacter.isFemale());
}

TEST(ImperatorWorld_CharacterTests, traitsCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	const std::vector<std::string> traitsVector{ "lustful", "submissive", "greedy" };

	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\ttraits = { \"lustful\" \"submissive\" \"greedy\" }";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(traitsVector, theCharacter.getTraits());
}

TEST(ImperatorWorld_CharacterTests, traitsDefaultToEmpty)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_TRUE(theCharacter.getTraits().empty());
}

TEST(ImperatorWorld_CharacterTests, birthDateCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tbirth_date=408.6.28";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(date("408.6.28"), theCharacter.getBirthDate());
}

TEST(ImperatorWorld_CharacterTests, birthDateDefaultsTo1_1_1)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(date("1.1.1"), theCharacter.getDeathDate());
}

TEST(ImperatorWorld_CharacterTests, deathDateCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tdeath_date=408.6.28";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(date("408.6.28"), theCharacter.getDeathDate());
}

TEST(ImperatorWorld_CharacterTests, deathDateDefaultsTo1_1_1)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(date("1.1.1"), theCharacter.getBirthDate());
}

TEST(ImperatorWorld_CharacterTests, spousesCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tspouse= { 69 420 } ";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_FALSE(theCharacter.getSpouses().empty());
	ASSERT_EQ(69, theCharacter.getSpouses().find(69)->first);
	ASSERT_EQ(420, theCharacter.getSpouses().find(420)->first);
}

TEST(ImperatorWorld_CharacterTests, spousesDefaultToEmpty)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_TRUE(theCharacter.getSpouses().empty());
}

TEST(ImperatorWorld_CharacterTests, childrenCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tchildren = { 69 420 } ";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_FALSE(theCharacter.getChildren().empty());
	ASSERT_EQ(69, theCharacter.getChildren().find(69)->first);
	ASSERT_EQ(420, theCharacter.getChildren().find(420)->first);
}

TEST(ImperatorWorld_CharacterTests, childrenDefaultToEmpty)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_TRUE(theCharacter.getChildren().empty());
}

TEST(ImperatorWorld_CharacterTests, motherCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tmother=123";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(123, theCharacter.getMother().first);
}

TEST(ImperatorWorld_CharacterTests, motherDefaultsToZero)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(0, theCharacter.getMother().first);
}

TEST(ImperatorWorld_CharacterTests, fatherCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tfather=123";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(123, theCharacter.getFather().first);
}

TEST(ImperatorWorld_CharacterTests, fatherDefaultsToZero)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(0, theCharacter.getFather().first);
}

TEST(ImperatorWorld_CharacterTests, familyCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tfamily=123";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(123, theCharacter.getFamily().first);
}

TEST(ImperatorWorld_CharacterTests, familyDefaultsToZero)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(0, theCharacter.getFamily().first);
}
TEST(ImperatorWorld_CharacterTests, wealthCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "wealth=\"420.5\"";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_NEAR(420.5, theCharacter.getWealth(), 0.001);
}

TEST(ImperatorWorld_CharacterTests, wealthDefaultsToZero)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(0, theCharacter.getWealth());
}

TEST(ImperatorWorld_CharacterTests, nameCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "first_name_loc = {\n";
	input << "name=\"Biggus Dickus\"\n";
	input << "}\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ("Biggus Dickus", theCharacter.getName());
}

TEST(ImperatorWorld_CharacterTests, nameDefaultsToBlank)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_TRUE(theCharacter.getName().empty());
}

TEST(ImperatorWorld_CharacterTests, attributesDefaultToZero)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(0, theCharacter.getAttributes().martial);
	ASSERT_EQ(0, theCharacter.getAttributes().finesse);
	ASSERT_EQ(0, theCharacter.getAttributes().charisma);
	ASSERT_EQ(0, theCharacter.getAttributes().zeal);
}

TEST(ImperatorWorld_CharacterTests, attributesCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tattributes={ martial=1 finesse=2 charisma=3 zeal=4 }";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(1, theCharacter.getAttributes().martial);
	ASSERT_EQ(2, theCharacter.getAttributes().finesse);
	ASSERT_EQ(3, theCharacter.getAttributes().charisma);
	ASSERT_EQ(4, theCharacter.getAttributes().zeal);
}

TEST(ImperatorWorld_CharacterTests, cultureCanBeInheritedFromFamily)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
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

	const ImperatorWorld::Family theFamily(familyInput, 42);
	ImperatorWorld::Character theCharacter(characterInput, 69, genesDB, endDate);

	if (theCharacter.getFamily().first == theFamily.getID())
	{
		auto familyPointer = std::make_shared<ImperatorWorld::Family>(theFamily);
		theCharacter.setFamily(familyPointer);
	}

	ASSERT_EQ("paradoxian", theCharacter.getCulture());
}


TEST(ImperatorWorld_CharacterTests, dnaCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tdna=\"paradoxian\"";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ("paradoxian", theCharacter.getDNA());
}

TEST(ImperatorWorld_CharacterTests, dnaDefaultsToNullopt)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_FALSE(theCharacter.getDNA());
}


TEST(ImperatorWorld_CharacterTests, portraitDataIsNotExtractedFromDnaOfWrongLength)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "={dna=\"AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/==\"}";
	const ImperatorWorld::Character character(input, 42, genesDB, endDate);

	ASSERT_FALSE(character.getPortraitData());
}


TEST(ImperatorWorld_CharacterTests, colorPaletteCoordinatesCanBeExtractedFromDNA)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "={dna=\"AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==\"}";
	const ImperatorWorld::Character character(input, 42, genesDB, endDate);

	ASSERT_EQ(0, character.getPortraitData().value().getHairColorPaletteCoordinates().x);
	ASSERT_EQ(0, character.getPortraitData().value().getHairColorPaletteCoordinates().y);
	ASSERT_EQ(0, character.getPortraitData().value().getSkinColorPaletteCoordinates().x);
	ASSERT_EQ(0, character.getPortraitData().value().getSkinColorPaletteCoordinates().y);
	ASSERT_EQ(0, character.getPortraitData().value().getEyeColorPaletteCoordinates().x);
	ASSERT_EQ(0, character.getPortraitData().value().getEyeColorPaletteCoordinates().y);
}


TEST(ImperatorWorld_CharacterTests, ageCanBeSet)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tage=56\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(56, theCharacter.getAge());
}
TEST(ImperatorWorld_CharacterTests, ageDefaultsToMale)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);

	ASSERT_EQ(0, theCharacter.getAge());
}


TEST(ImperatorWorld_CharacterTests, getAgeSexReturnsCorrectString)
{
	const ImperatorWorld::GenesDB genesDB;
	const date endDate;
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

	const ImperatorWorld::Character theCharacter(input, 42, genesDB, endDate);
	const ImperatorWorld::Character theCharacter2(input2, 43, genesDB, endDate);
	const ImperatorWorld::Character theCharacter3(input3, 44, genesDB, endDate);
	const ImperatorWorld::Character theCharacter4(input4, 45, genesDB, endDate);

	ASSERT_EQ("female", theCharacter.getAgeSex());
	ASSERT_EQ("male", theCharacter2.getAgeSex());
	ASSERT_EQ("girl", theCharacter3.getAgeSex());
	ASSERT_EQ("boy", theCharacter4.getAgeSex());
}