#include "CharacterFactory.h"
#include "CharacterName.h"
#include "CharacterAttributes.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



Imperator::Character::Factory::Factory() {
	registerKeyword("first_name_loc", [this](std::istream& theStream) {
		character->name = CharacterName(theStream).getName();
	});
	registerKeyword("province", [this](std::istream& theStream) {
		character->province = commonItems::getULlong(theStream);
	});
	registerKeyword("culture", [this](std::istream& theStream) {
		character->culture = commonItems::getString(theStream);
	});
	registerKeyword("religion", [this](std::istream& theStream) {
		character->religion = commonItems::getString(theStream);
	});
	registerKeyword("female", [this](std::istream& theStream) {
		const auto femStr = commonItems::getString(theStream);
		character->female = femStr == "yes";
	});
	registerKeyword("traits", [this](std::istream& theStream) {
		character->traits = commonItems::getStrings(theStream);
	});
	registerKeyword("birth_date", [this](std::istream& theStream) {
		const auto dateStr = commonItems::getString(theStream);
		character->birthDate = date(dateStr, true); // converted to AD
	});
	registerKeyword("death_date", [this](std::istream& theStream) {
		const auto dateStr = commonItems::getString(theStream);
		character->deathDate = date(dateStr, true); // converted to AD
	});
	registerKeyword("age", [this](std::istream& theStream) {
		character->age = static_cast<unsigned int>(commonItems::getInt(theStream));
	});
	registerKeyword("nickname", [this](std::istream& theStream) {
		character->nickname = commonItems::getString(theStream);
	});
	registerKeyword("family", [this](std::istream& theStream) {
		character->family = std::pair(commonItems::getULlong(theStream), nullptr);
	});
	registerKeyword("dna", [this](std::istream& theStream) {
		character->dna = commonItems::getString(theStream);
	});
	registerKeyword("mother", [this](std::istream& theStream) {
		character->mother = std::pair(commonItems::getULlong(theStream), nullptr);
	});
	registerKeyword("father", [this](std::istream& theStream) {
		character->father = std::pair(commonItems::getULlong(theStream), nullptr);
	});
	registerKeyword("wealth", [this](std::istream& theStream) {
		character->wealth = commonItems::getDouble(theStream);
	});
	registerKeyword("spouse", [this](std::istream& theStream) {
		for (const auto spouse : commonItems::getULlongs(theStream))
			character->spouses.emplace(spouse, nullptr);
	});
	registerKeyword("children", [this](std::istream& theStream) {
		for (const auto child : commonItems::getULlongs(theStream))
			character->children.emplace(child, nullptr);
	});
	registerKeyword("attributes", [this](std::istream& theStream) {
		const CharacterAttributes attributesFromBloc(theStream);
		character->attributes.martial = attributesFromBloc.getMartial();
		character->attributes.finesse = attributesFromBloc.getFinesse();
		character->attributes.charisma = attributesFromBloc.getCharisma();
		character->attributes.zeal = attributesFromBloc.getZeal();
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


std::unique_ptr<Imperator::Character> Imperator::Character::Factory::getCharacter(std::istream& theStream, const std::string& idString, const std::shared_ptr<GenesDB>& genesDB) {
	character = std::make_unique<Character>();
	character->ID = std::stoull(idString);
	character->genes = genesDB;

	parseStream(theStream);

	if (character->dna && character->dna.value().size() == 552) {
		character->portraitData.emplace(CharacterPortraitData(character->dna.value(), character->genes, character->getAgeSex()));
	}

	return std::move(character);
}