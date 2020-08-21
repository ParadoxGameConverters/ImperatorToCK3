#ifndef IMPERATOR_WORLD
#define IMPERATOR_WORLD


#include "GameVersion.h"
#include "Date.h"
#include "Families/Families.h"
#include "Characters/Characters.h"
#include "Provinces/Pops.h"
#include "Provinces/Provinces.h"
#include "Countries/Countries.h"
#include "Parser.h"
#include <string>
#include <set>


class Configuration;



namespace ImperatorWorld
{
	class World: commonItems::parser
	{
		public:
			World(const Configuration& theConfiguration);
			std::string getSaveName() const { return "CK3tester"; }

		private:
			void verifySave(const std::string& saveGamePath);
			bool uncompressSave(const std::string& saveGamePath);

			date startDate = date("450.10.1");
			date endDate = date("727.2.17");
			GameVersion ImperatorVersion;
			std::set<std::string> DLCs;
			std::set<std::string> Mods;

			struct saveData {
				bool compressed = false;
				std::string metadata;
				std::string gamestate;
			};
			saveData saveGame;

			Families families;
			Characters characters;
			Pops pops;
			Provinces provinces;
			Countries countries;
	};		
}



#endif // IMPERATOR_WORLD
