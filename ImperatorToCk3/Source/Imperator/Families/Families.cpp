#include "Families.h"
#include "Family.h"
#include "Log.h"
#include "ParserHelpers.h"



void ImperatorWorld::Families::loadFamilies(const std::string& thePath)
{
	registerKeys();
	parseFile(thePath);
	clearRegisteredKeywords();
}
void ImperatorWorld::Families::loadFamilies(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Families::registerKeys()
{
	registerRegex("\\d+", [this](const std::string& theFamilyID, std::istream& theStream) {
		if (families.count(std::stoi(theFamilyID))) {
			families[std::stoi(theFamilyID)]->updateFamily(theStream);
		}
		else {
			auto newFamily = std::make_shared<Family>(theStream, std::stoi(theFamilyID));
			families.insert(std::pair(newFamily->getID(), newFamily));
			if ((newFamily->getID()) % 25 == 0) LOG(LogLevel::Info) << ">> [debug] Read family " << newFamily->getID() << newFamily->getCulture() << newFamily->getKey();
		}
	});
	registerRegex("[A-Za-z0-9\\_:.-]+", commonItems::ignoreItem);
}



void ImperatorWorld::FamiliesBloc::loadFamiliesBloc(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::FamiliesBloc::registerKeys()
{
	registerKeyword("families", [this](const std::string& unused, std::istream& theStream) {
		families.loadFamilies(theStream);
	});
	registerRegex("[A-Za-z0-9\\_:.-]+", commonItems::ignoreItem);
}