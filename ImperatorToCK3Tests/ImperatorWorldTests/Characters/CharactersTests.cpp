#include "gtest/gtest.h"
#include <sstream>
#include "Imperator/Characters/Characters.h"
#include "Imperator/Characters/Character.h"
#include "Imperator/Genes/GenesDB.h"



TEST(ImperatorWorld_CharactersTests, charactersDefaultToEmpty) {
	const auto genes = std::make_shared<Imperator::GenesDB>();
	
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::Characters characters(input, genes);

	ASSERT_TRUE(characters.getCharacters().empty());
}

TEST(ImperatorWorld_CharactersTests, charactersCanBeLoaded) {
	const auto genes = std::make_shared<Imperator::GenesDB>();
	
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42={}\n";
	input << "43={}\n";
	input << "}";

	const Imperator::Characters characters(input, genes);
	
	const auto& characterItr = characters.getCharacters().find(42);
	const auto& characterItr2 = characters.getCharacters().find(43);

	ASSERT_EQ(42, characterItr->first);
	ASSERT_EQ(42, characterItr->second->getID());
	ASSERT_EQ(43, characterItr2->first);
	ASSERT_EQ(43, characterItr2->second->getID());
}