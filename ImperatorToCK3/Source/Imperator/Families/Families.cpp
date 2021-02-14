#include "Families.h"
#include "Family.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



void Imperator::Families::loadFamilies(const std::string& thePath)
{
	registerKeys();
	parseFile(thePath);
	clearRegisteredKeywords();
}
void Imperator::Families::loadFamilies(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::Families::registerKeys()
{
	registerMatcher(commonItems::integerMatch, [this](const std::string& theFamilyID, std::istream& theStream) {
		const auto familyStr = commonItems::stringOfItem(theStream).getString();
		if (familyStr.find('{') != std::string::npos)
		{
			std::stringstream tempStream(familyStr);
			if (families.contains(std::stoull(theFamilyID))) {
				families[std::stoull(theFamilyID)]->updateFamily(tempStream);
			}
			else {
				auto newFamily = std::make_shared<Family>(tempStream, std::stoull(theFamilyID));
				families.insert(std::pair(newFamily->getID(), newFamily));
			}
		}
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}



Imperator::FamiliesBloc::FamiliesBloc(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::FamiliesBloc::registerKeys()
{
	registerKeyword("families", [this](std::istream& theStream) {
		families.loadFamilies(theStream);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}