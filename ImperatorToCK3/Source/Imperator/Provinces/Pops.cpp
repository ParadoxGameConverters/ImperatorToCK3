#include "Pops.h"
#include "Pop.h"
#include "PopFactory.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"


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
			auto pop = popFactory.getPop(thePopID, tempStream);
			pops.insert(std::pair(pop->ID, std::move(pop)));
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
	registerKeyword("population", [this](std::istream& theStream) {
		pops.loadPops(theStream);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}