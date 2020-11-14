#include "Character.h"
#include <utility>
#include "../Families/Family.h"
#include "ParserHelpers.h"
#include "CharacterName.h"
#include "CharacterAttributes.h"
#include "PortraitData.h"
#include "Log.h"



Imperator::Character::Character(std::istream& theStream, const unsigned long long chrID, GenesDB genesDB, const date& _endDate) : charID(chrID), genes(std::move(genesDB)), endDate(_endDate)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();

	if (dna && dna.value().size() == 552) portraitData.emplace(CharacterPortraitData(dna.value(), genes, getAgeSex()));
}

void Imperator::Character::registerKeys()
{
	registerRegex("first_name_loc", [this](const std::string& unused, std::istream& theStream) {
		name = CharacterName(theStream).getName();
	});
	registerRegex("province", [this](const std::string& unused, std::istream& theStream) {
		province = commonItems::singleULlong(theStream).getULlong();
	});
	registerKeyword("culture", [this](const std::string& unused, std::istream& theStream) {
		culture = commonItems::singleString(theStream).getString();
	});
	registerKeyword("religion", [this](const std::string& unused, std::istream& theStream) {
		religion = commonItems::singleString(theStream).getString();
	});
	registerKeyword("female", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString femStr(theStream);
		female = femStr.getString() == "yes";
	});
	registerKeyword("traits", [this](const std::string& unused, std::istream& theStream) {
		traits = commonItems::stringList(theStream).getStrings();
	});
	registerKeyword("birth_date", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString dateStr(theStream);
		birthDate = date(dateStr.getString(), true); // converted to AD
	});
	registerKeyword("death_date", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString dateStr(theStream);
		deathDate = date(dateStr.getString(), true); // converted to AD
	});
	registerKeyword("age", [this](const std::string& unused, std::istream& theStream) {
		age = static_cast<unsigned int>(commonItems::singleInt(theStream).getInt());
	});
	registerKeyword("nickname", [this](const std::string& unused, std::istream& theStream) {
		nickname = commonItems::singleString(theStream).getString();
	});
	registerKeyword("family", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleULlong familyLLong(theStream);
		family = std::pair(familyLLong.getULlong(), nullptr);
	});
	registerKeyword("dna", [this](const std::string& unused, std::istream& theStream) {
		dna = commonItems::singleString(theStream).getString();
	});
	registerKeyword("mother", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleULlong motherLLong(theStream);
		mother = std::pair(motherLLong.getULlong(), nullptr);
	});
	registerKeyword("father", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleULlong fatherLLong(theStream);
		father = std::pair(fatherLLong.getULlong(), nullptr);
	});
	registerKeyword("wealth", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleDouble wealthDbl(theStream);
		wealth = wealthDbl.getDouble();
	});
	registerKeyword("spouse", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::ullongList spouseList(theStream);
		for (const auto spouse : spouseList.getULlongs())
			spouses.emplace(spouse, nullptr);
	});
	registerKeyword("children", [this](const std::string& unused, std::istream& theStream) {
		for (const auto child : commonItems::ullongList(theStream).getULlongs())
			children.emplace(child, nullptr);
	});
	registerRegex("attributes", [this](const std::string& unused, std::istream& theStream) {
		const CharacterAttributes attributesFromBloc(theStream);
		attributes.martial = attributesFromBloc.getMartial();
		attributes.finesse = attributesFromBloc.getFinesse();
		attributes.charisma = attributesFromBloc.getCharisma();
		attributes.zeal = attributesFromBloc.getZeal();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


const std::string& Imperator::Character::getCulture() const
{
	if (!culture.empty())
		return culture;
	if (family.first && !family.second->getCulture().empty())
		return family.second->getCulture();
	return culture;
}

std::string Imperator::Character::getAgeSex() const
{
	if (age >= 16)
	{
		if (female) return "female";
		return "male";
	}
	if (female) return "girl";
	return "boy";
}
