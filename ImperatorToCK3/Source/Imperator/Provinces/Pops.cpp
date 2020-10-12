#include "Pops.h"
#include "Pop.h"
#include "Log.h"
#include "ParserHelpers.h"


void Imperator::Pops::loadPops(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::Pops::registerKeys()
{
	registerRegex("\\d+", [this](const std::string& thePopID, std::istream& theStream) {
		const auto popStr = commonItems::stringOfItem(theStream).getString();
		if (popStr.find('{') != std::string::npos)
		{
			std::stringstream tempStream(popStr);
			auto pop = std::make_shared<Pop>(tempStream, std::stoi(thePopID));
			pops.insert(std::pair(pop->getID(), pop));
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}



Imperator::PopsBloc::PopsBloc(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::PopsBloc::registerKeys()
{
	registerKeyword("population", [this](const std::string& unused, std::istream& theStream) {
		pops.loadPops(theStream);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}