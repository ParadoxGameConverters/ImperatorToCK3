#include "CharacterFactory.h"
#include "CharacterName.h"
#include "CharacterAttributes.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



Imperator::Character::Factory::Factory()
{
	registerKeyword("first_name_loc", [this](std::istream& theStream) {
		character->name = CharacterName(theStream).getName();
		});
	registerKeyword("province", [this](std::istream& theStream) {
		character->province = commonItems::singleULlong(theStream).getULlong();
		});
	registerKeyword("culture", [this](std::istream& theStream) {
		character->culture = commonItems::singleString(theStream).getString();
		});
	registerKeyword("religion", [this](std::istream& theStream) {
		character->religion = commonItems::singleString(theStream).getString();
		});
	registerKeyword("female", [this](std::istream& theStream) {
		const commonItems::singleString femStr(theStream);
		character->female = femStr.getString() == "yes";
		});
	registerKeyword("traits", [this](std::istream& theStream) {
		character->traits = commonItems::stringList(theStream).getStrings();
		});
	registerKeyword("birth_date", [this](std::istream& theStream) {
		const commonItems::singleString dateStr(theStream);
		character->birthDate = date(dateStr.getString(), true); // converted to AD
		});
	registerKeyword("death_date", [this](std::istream& theStream) {
		const commonItems::singleString dateStr(theStream);
		character->deathDate = date(dateStr.getString(), true); // converted to AD
		});
	registerKeyword("age", [this](std::istream& theStream) {
		character->age = static_cast<unsigned int>(commonItems::singleInt(theStream).getInt());
		});
	registerKeyword("nickname", [this](std::istream& theStream) {
		character->nickname = commonItems::singleString(theStream).getString();
		});
	registerKeyword("family", [this](std::istream& theStream) {
		const commonItems::singleULlong familyLLong(theStream);
		character->family = std::pair(familyLLong.getULlong(), nullptr);
		});
	registerKeyword("dna", [this](std::istream& theStream) {
		character->dna = commonItems::singleString(theStream).getString();
		});
	registerKeyword("mother", [this](std::istream& theStream) {
		const commonItems::singleULlong motherLLong(theStream);
		character->mother = std::pair(motherLLong.getULlong(), nullptr);
		});
	registerKeyword("father", [this](std::istream& theStream) {
		const commonItems::singleULlong fatherLLong(theStream);
		character->father = std::pair(fatherLLong.getULlong(), nullptr);
		});
	registerKeyword("wealth", [this](std::istream& theStream) {
		const commonItems::singleDouble wealthDbl(theStream);
		character->wealth = wealthDbl.getDouble();
		});
	registerKeyword("spouse", [this](std::istream& theStream) {
		const commonItems::ullongList spouseList(theStream);
		for (const auto spouse : spouseList.getULlongs())
			character->spouses.emplace(spouse, nullptr);
		});
	registerKeyword("children", [this](std::istream& theStream) {
		for (const auto child : commonItems::ullongList(theStream).getULlongs())
			character->children.emplace(child, nullptr);
		});
	registerKeyword("attributes", [this](std::istream& theStream) {
		const CharacterAttributes attributesFromBloc(theStream);
		character->attributes.martial = attributesFromBloc.getMartial();
		character->attributes.finesse = attributesFromBloc.getFinesse();
		character->attributes.charisma = attributesFromBloc.getCharisma();
		character->attributes.zeal = attributesFromBloc.getZeal();
		});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


std::unique_ptr<Imperator::Character> Imperator::Character::Factory::getCharacter(std::istream& theStream, const std::string& idString, const std::shared_ptr<GenesDB>& genesDB)
{
	character = std::make_unique<Character>();
	character->ID = std::stoull(idString);
	character->genes = genesDB;

	parseStream(theStream);

	if (character->dna && character->dna.value().size() == 552) character->portraitData.emplace(CharacterPortraitData(character->dna.value(), character->genes, character->getAgeSex()));

	return std::move(character);
}