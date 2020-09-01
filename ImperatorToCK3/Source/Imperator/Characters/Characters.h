#ifndef IMPERATOR_CHARACTERS_H
#define IMPERATOR_CHARACTERS_H

#include "Parser.h"
#include "../Genes/GenesDB.h"
#include "../../Configuration/Configuration.h"
#include "Date.h"

namespace ImperatorWorld
{
	class Families;
	class Character;
	class Characters: commonItems::parser
	{
	  public:
		Characters() = default;
		Characters(std::istream& theStream, const GenesDB& genesDB, const date& _endDate);

		[[nodiscard]] const auto& getCharacters() const { return characters; }

		void linkFamilies(const Families& theFamilies);
		void linkSpouses();
		void linkMothersAndFathers();

	  private:
		void registerKeys();

		GenesDB genes;
		date endDate;

		std::map<int, std::shared_ptr<Character>> characters;
	}; // class Characters

	class CharactersBloc : commonItems::parser
	{
	public:
		CharactersBloc() = default;
		explicit CharactersBloc(std::istream& theStream, const GenesDB& genesDB, const date& _endDate);

		[[nodiscard]] const auto& getCharactersFromBloc() const { return characters; }

	private:
		void registerKeys();

		GenesDB genes;
		date endDate;
		Characters characters;
	}; // class CharactersBloc
} // namespace ImperatorWorld

#endif // IMPERATOR_CHARACTERS_H
