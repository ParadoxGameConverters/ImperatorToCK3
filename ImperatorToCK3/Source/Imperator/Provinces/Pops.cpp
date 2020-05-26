#include "Pops.h"
#include "Pop.h"
#include "Log.h"
#include "ParserHelpers.h"


void ImperatorWorld::Pops::loadPops(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Pops::registerKeys()
{
	registerRegex("\\d+", [this](const std::string& thePopID, std::istream& theStream) {
		const auto popStr = commonItems::singleItem(thePopID, theStream);
		if (popStr.find('{') != std::string::npos)
		{
			std::stringstream tempStream(popStr);
			auto pop = std::make_shared<Pop>(tempStream, std::stoi(thePopID));
			pops.insert(std::pair(pop->getID(), pop));
		}
	});
	registerRegex("[A-Za-z0-9\\_:.-]+", commonItems::ignoreItem);
}



ImperatorWorld::PopsBloc::PopsBloc(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::PopsBloc::registerKeys()
{
	registerKeyword("population", [this](const std::string& unused, std::istream& theStream) {
		pops.loadPops(theStream);
	});
	registerRegex("[A-Za-z0-9\\_:.-]+", commonItems::ignoreItem);
}