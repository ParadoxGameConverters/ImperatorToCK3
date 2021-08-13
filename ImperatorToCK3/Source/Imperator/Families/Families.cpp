#include "Families.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



void Imperator::Families::loadFamilies(const std::string& thePath) {
	registerKeys();
	parseFile(thePath);
	clearRegisteredKeywords();
}


void Imperator::Families::loadFamilies(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void Imperator::Families::removeUnlinkedMembers() {
	for (const auto& family : families) {
		family.second->removeUnlinkedMembers();
	}
}


void Imperator::Families::registerKeys() {
	registerRegex(commonItems::integerRegex, [this](const std::string& theFamilyID, std::istream& theStream) {
		const auto familyStr = commonItems::stringOfItem(theStream).getString();
		if (familyStr.find('{') != std::string::npos) {
			std::stringstream tempStream(familyStr);
			const auto ID = commonItems::stringToInteger<unsigned long long>(theFamilyID);
			std::shared_ptr<Family> newFamily = familyFactory.getFamily(tempStream, ID);
			auto [iterator, inserted] = families.emplace(newFamily->getID(), newFamily);
			if (!inserted) {
				Log(LogLevel::Debug) << "Redefinition of family " << theFamilyID;
				iterator->second = newFamily;
			}
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}



Imperator::FamiliesBloc::FamiliesBloc(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void Imperator::FamiliesBloc::registerKeys() {
	registerKeyword("families", [this](std::istream& theStream) {
		families.loadFamilies(theStream);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}