#ifndef IMPERATOR_WORLD
#define IMPERATOR_WORLD


#include "../Common/Version.h"
#include "Date.h"
#include "newParser.h"
#include <string>


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
		Version ImperatorVersion;

		struct saveData {
			bool compressed = false;
			std::string metadata;
			std::string gamestate;
		};
		saveData saveGame;
};		
}



#endif // IMPERATOR_WORLD
