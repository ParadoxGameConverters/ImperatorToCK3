#ifndef IMPERATOR_CHARACTERS_H
#define IMPERATOR_CHARACTERS_H

#include "Parser.h"
#include "../Genes/GenesDB.h"
#include "CharacterFactory.h"
#include "Date.h"

namespace Imperator
{
	class Families;
	class Character;
	class Characters: commonItems::parser
	{
	  public:
		Characters() = default;
		Characters(std::istream& theStream, std::shared_ptr<GenesDB> genesDB);

		Characters& operator= (const Characters& obj) { this->characters = obj.characters; return *this; }

		[[nodiscard]] const auto& getCharacters() const { return characters; }

		void linkFamilies(const Families& theFamilies);
		void linkSpouses();
		void linkMothersAndFathers();

	  private:
		void registerKeys();

		Character::Factory characterFactory;

		std::shared_ptr<GenesDB> genes;
		//std::shared_ptr<date> endDate;

		std::map<unsigned long long, std::shared_ptr<Character>> characters;
	}; // class Characters

	class CharactersBloc : commonItems::parser
	{
	public:
		CharactersBloc() = default;
		explicit CharactersBloc(std::istream& theStream, const GenesDB& genesDB);

		[[nodiscard]] const auto& getCharactersFromBloc() const { return characters; }

	private:
		void registerKeys();

		std::shared_ptr<GenesDB> genes;
		Characters characters;
	}; // class CharactersBloc
} // namespace Imperator

#endif // IMPERATOR_CHARACTERS_H
