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
		const auto familyStr = commonItems::singleItem(theFamilyID, theStream);
		if (familyStr.find('{') != std::string::npos)
		{
			std::stringstream tempStream(familyStr);
			if (families.count(std::stoi(theFamilyID))) {
				families[std::stoi(theFamilyID)]->updateFamily(tempStream);
			}
			else {
				auto newFamily = std::make_shared<Family>(tempStream, std::stoi(theFamilyID));
				families.insert(std::pair(newFamily->getID(), newFamily));
			}
		}
		else LOG(LogLevel::Info) << "-> Didn't load family " << theFamilyID << ", it's probably \"none\"";
	});
	registerRegex("[A-Za-z0-9\\_:.-]+", commonItems::ignoreItem);
}



ImperatorWorld::FamiliesBloc::FamiliesBloc(std::istream& theStream)
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