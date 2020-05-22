#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Characters/Character.h"
#include "../ImperatorToCK3/Source/Imperator/Families/Family.h"
#include "../commonItems/Date.h"
#include <sstream>

TEST(ImperatorWorld_CharacterTests, IDCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(42, theCharacter.getID());
}

TEST(ImperatorWorld_CharacterTests, cultureCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tculture=\"paradoxian\"";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ("paradoxian", theCharacter.getCulture());
}

TEST(ImperatorWorld_CharacterTests, cultureDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_TRUE(theCharacter.getCulture().empty());
}


TEST(ImperatorWorld_CharacterTests, religionCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\treligion=\"paradoxian\"";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ("paradoxian", theCharacter.getReligion());
}

TEST(ImperatorWorld_CharacterTests, religionDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_TRUE(theCharacter.getReligion().empty());
}

TEST(ImperatorWorld_CharacterTests, sexCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tfemale=yes";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_TRUE(theCharacter.isFemale());
}
TEST(ImperatorWorld_CharacterTests, sexDefaultsToMale)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_FALSE(theCharacter.isFemale());
}

TEST(ImperatorWorld_CharacterTests, traitsCanBeSet)
{
	std::vector<std::string> traitsVector{ "lustful", "submissive", "greedy" };

	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\ttraits = { \"lustful\" \"submissive\" \"greedy\" }";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(traitsVector, theCharacter.getTraits());
}

TEST(ImperatorWorld_CharacterTests, traitsDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_TRUE(theCharacter.getTraits().empty());
}

TEST(ImperatorWorld_CharacterTests, birthDateCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tbirth_date=408.6.28";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(date("408.6.28"), theCharacter.getBirthDate());
}

TEST(ImperatorWorld_CharacterTests, birthDateDefaultsTo1_1_1)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(date("1.1.1"), theCharacter.getDeathDate());
}

TEST(ImperatorWorld_CharacterTests, deathDateCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tdeath_date=408.6.28";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(date("408.6.28"), theCharacter.getDeathDate());
}

TEST(ImperatorWorld_CharacterTests, deathDateDefaultsTo1_1_1)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(date("1.1.1"), theCharacter.getBirthDate());
}

TEST(ImperatorWorld_CharacterTests, spousesCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tspouse= { 69 420 } ";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_FALSE(theCharacter.getSpouses().empty());
	ASSERT_EQ(69, theCharacter.getSpouses().find(69)->first);
	ASSERT_EQ(420, theCharacter.getSpouses().find(420)->first);
}

TEST(ImperatorWorld_CharacterTests, spousesDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_TRUE(theCharacter.getSpouses().empty());
}

TEST(ImperatorWorld_CharacterTests, childrenCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tchildren = { 69 420 } ";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_FALSE(theCharacter.getChildren().empty());
	ASSERT_EQ(69, theCharacter.getChildren().find(69)->first);
	ASSERT_EQ(420, theCharacter.getChildren().find(420)->first);
}

TEST(ImperatorWorld_CharacterTests, childrenDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_TRUE(theCharacter.getChildren().empty());
}

TEST(ImperatorWorld_CharacterTests, motherCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tmother=123";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(123, theCharacter.getMother().first);
}

TEST(ImperatorWorld_CharacterTests, motherDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(0, theCharacter.getMother().first);
}

TEST(ImperatorWorld_CharacterTests, fatherCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tfather=123";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(123, theCharacter.getFather().first);
}

TEST(ImperatorWorld_CharacterTests, fatherDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(0, theCharacter.getFather().first);
}

TEST(ImperatorWorld_CharacterTests, familyCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tfamily=123";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(123, theCharacter.getFamily().first);
}

TEST(ImperatorWorld_CharacterTests, familyDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(theCharacter.getFamily().first, 0);
}
TEST(ImperatorWorld_CharacterTests, wealthCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "wealth=\"420.5\"";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_NEAR(420.5, theCharacter.getWealth(), 0.001);
}

TEST(ImperatorWorld_CharacterTests, wealthDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(0, theCharacter.getWealth());
}

TEST(ImperatorWorld_CharacterTests, nameCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "first_name_loc = {\n";
	input << "name=\"Biggus Dickus\"\n";
	input << "}\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ("Biggus Dickus", theCharacter.getName());
}

TEST(ImperatorWorld_CharacterTests, nameDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_TRUE(theCharacter.getName().empty());
}

TEST(ImperatorWorld_CharacterTests, attributesDefaultToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(0, theCharacter.getAttributes().martial);
	ASSERT_EQ(0, theCharacter.getAttributes().finesse);
	ASSERT_EQ(0, theCharacter.getAttributes().charisma);
	ASSERT_EQ(0, theCharacter.getAttributes().zeal);
}

TEST(ImperatorWorld_CharacterTests, attributesCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tattributes={ martial=1 finesse=2 charisma=3 zeal=4 }";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(1, theCharacter.getAttributes().martial);
	ASSERT_EQ(2, theCharacter.getAttributes().finesse);
	ASSERT_EQ(3, theCharacter.getAttributes().charisma);
	ASSERT_EQ(4, theCharacter.getAttributes().zeal);
}

TEST(ImperatorWorld_CharacterTests, cultureCanBeInheritedFromFamily)
{
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
	ImperatorWorld::Character theCharacter(characterInput, 69);

	if (theCharacter.getFamily().first == theFamily.getID())
	{
		auto familyPointer = std::make_shared<ImperatorWorld::Family>(theFamily);
		theCharacter.setFamily(familyPointer);
	}

	ASSERT_EQ("paradoxian", theCharacter.getCulture());
}