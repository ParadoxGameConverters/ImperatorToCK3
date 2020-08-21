#include "Character.h"
#include "../Families/Family.h"
#include "ParserHelpers.h"
#include "CharacterName.h"
#include "CharacterAttributes.h"

ImperatorWorld::Character::Character(std::istream& theStream, int chrID): charID(chrID)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Character::registerKeys()
{
	registerRegex("first_name_loc", [this](const std::string& unused, std::istream& theStream) {
		name = CharacterName(theStream).getName();
	});
	registerKeyword("culture", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString cultureStr(theStream);
		culture = cultureStr.getString();
	});
	registerKeyword("religion", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString religionStr(theStream);
		religion = religionStr.getString();
	});
	registerKeyword("female", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString femStr(theStream);
		female = femStr.getString() == "yes";
	});
	registerKeyword("traits", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::stringList trList(theStream);
		traits = trList.getStrings();
	});
	registerKeyword("birth_date", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString dateStr(theStream);
		birthDate = date(dateStr.getString());
	});
	registerKeyword("death_date", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString dateStr(theStream);
		deathDate = date(dateStr.getString());
	});
	registerKeyword("family", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt familyInt(theStream);
		family = std::pair(familyInt.getInt(), nullptr);
	});
	registerKeyword("mother", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt motInt(theStream);
		mother = std::pair(motInt.getInt(), nullptr);
	});
	registerKeyword("father", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt fatInt(theStream);
		father = std::pair(fatInt.getInt(), nullptr);
	});
	registerKeyword("wealth", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleDouble wealthDbl(theStream);
		wealth = wealthDbl.getDouble();
	});
	registerKeyword("spouse", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::intList spouseList(theStream);
		for (const auto spouse : spouseList.getInts())
			spouses.insert(std::pair(spouse, nullptr));
	});
	registerKeyword("children", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::intList childrenList(theStream);
		for (const auto child : childrenList.getInts())
			children.insert(std::pair(child, nullptr));
	});
	registerRegex("attributes", [this](const std::string& unused, std::istream& theStream) {
		CharacterAttributes attributesFromBloc(theStream);
		attributes.martial = attributesFromBloc.getMartial();
		attributes.finesse = attributesFromBloc.getFinesse();
		attributes.charisma = attributesFromBloc.getCharisma();
		attributes.zeal = attributesFromBloc.getZeal();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


const std::string& ImperatorWorld::Character::getCulture() const
{
	if (!culture.empty())
		return culture;
	if (family.first && !family.second->getCulture().empty())
		return family.second->getCulture();
	return culture;
}