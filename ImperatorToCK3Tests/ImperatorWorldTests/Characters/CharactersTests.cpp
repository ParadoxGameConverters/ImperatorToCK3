#include "gtest/gtest.h"
#include <sstream>

#include "../ImperatorToCK3/Source/Imperator/Characters/Characters.h"
#include "../ImperatorToCK3/Source/Imperator/Characters/Character.h"

TEST(ImperatorWorld_CharactersTests, charactersDefaultToEmpty)
{
	const ImperatorWorld::GenesDB genes;
	const date endDate;
	
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Characters characters(input, genes, endDate);

	ASSERT_TRUE(characters.getCharacters().empty());
}

TEST(ImperatorWorld_CharactersTests, charactersCanBeLoaded)
{
	const ImperatorWorld::GenesDB genes;
	const date endDate;
	
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42={}\n";
	input << "43={}\n";
	input << "}";

	const ImperatorWorld::Characters characters(input, genes, endDate);
	
	const auto& characterItr = characters.getCharacters().find(42);
	const auto& characterItr2 = characters.getCharacters().find(43);

	ASSERT_EQ(42, characterItr->first);
	ASSERT_EQ(42, characterItr->second->getID());
	ASSERT_EQ(43, characterItr2->first);
	ASSERT_EQ(43, characterItr2->second->getID());
}