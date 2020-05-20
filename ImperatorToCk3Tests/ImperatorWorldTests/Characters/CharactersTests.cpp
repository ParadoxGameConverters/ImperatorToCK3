#include "gtest/gtest.h"
#include <sstream>

#include "../ImperatorToCk3/Source/Imperator/Characters/Characters.h"
#include "../ImperatorToCk3/Source/Imperator/Characters/Character.h"

TEST(ImperatorWorld_CharactersTests, charactersDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Characters characters(input);

	ASSERT_TRUE(characters.getCharacters().empty());
}

TEST(ImperatorWorld_CharactersTests, charactersCanBeLoaded)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42={}\n";
	input << "43={}\n";
	input << "}";

	const ImperatorWorld::Characters characters(input);
	
	const auto& characterItr = characters.getCharacters().find(42);
	const auto& characterItr2 = characters.getCharacters().find(43);

	ASSERT_EQ(42, characterItr->first);
	ASSERT_EQ(42, characterItr->second->getID());
	ASSERT_EQ(43, characterItr2->first);
	ASSERT_EQ(43, characterItr2->second->getID());
}