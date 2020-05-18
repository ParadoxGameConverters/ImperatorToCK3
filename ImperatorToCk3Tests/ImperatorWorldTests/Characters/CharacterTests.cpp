#include "gtest/gtest.h"
#include "../ImperatorToCk3/Source/Imperator/Characters/Character.h"
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

TEST(ImperatorWorld_CharacterTests, wealthCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "wealth=\"420.5\"";
	input << "}";

	const ImperatorWorld::Character theCharacter(input, 42);

	ASSERT_EQ(420.5, theCharacter.getWealth());
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
