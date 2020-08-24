#include "Character.h"
#include "../Families/Family.h"
#include "ParserHelpers.h"
#include "CharacterName.h"
#include "CharacterAttributes.h"
#include "Log.h"
#include "base64.h"
#include <bitset>



long long binaryToDecimal(long long n)
{
	long long num = n;
	long long dec_value = 0;

	// Initializing base value to 1, i.e 2^0 
	int base = 1;

	long long temp = num;
	while (temp) {
		int last_digit = int(temp % 10);
		temp = temp / 10;

		dec_value += long long(last_digit * base);

		base = base * 2;
	}

	return dec_value;
}

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
	registerKeyword("dna", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString dnaStr(theStream);
		dna = dnaStr.getString();
		if (dna.size() == 552)
		{
			const std::string& hairStr = getDNA();
			const std::string& decodedDnaStr = base64_decode(hairStr);

			std::string binary_outputInformations;
			for (std::size_t i = 0; i < decodedDnaStr.size(); ++i)
			{
				std::bitset<8> b(decodedDnaStr.c_str()[i]);
				binary_outputInformations += b.to_string();
			}

			//hair
			portraitData.hairColorPaletteCoordinates.x = unsigned int (binaryToDecimal(stoll(binary_outputInformations.substr(0, 18))) / 512);
			portraitData.hairColorPaletteCoordinates.y = unsigned int (binaryToDecimal(stoll(binary_outputInformations.substr(0, 18))) % 512);
			//skin
			portraitData.skinColorPaletteCoordinates.x = unsigned int(binaryToDecimal(stoll(binary_outputInformations.substr(5, 18))) / 512);
			portraitData.skinColorPaletteCoordinates.y = unsigned int(binaryToDecimal(stoll(binary_outputInformations.substr(5, 18))) % 512);
			//eyes
			portraitData.eyeColorPaletteCoordinates.x = unsigned int(binaryToDecimal(stoll(binary_outputInformations.substr(10, 18))) / 512);
			portraitData.eyeColorPaletteCoordinates.y = unsigned int(binaryToDecimal(stoll(binary_outputInformations.substr(10, 18))) % 512);
			Log(LogLevel::Debug) << "Char ID " << charID << " hair color palette X Y coordinates: " << portraitData.hairColorPaletteCoordinates.x << " " << portraitData.hairColorPaletteCoordinates.y; // debug
			Log(LogLevel::Debug) << "Char ID " << charID << " skin color palette X Y coordinates: " << portraitData.skinColorPaletteCoordinates.x << " " << portraitData.skinColorPaletteCoordinates.y; // debug
			Log(LogLevel::Debug) << "Char ID " << charID << " eye color palette X Y coordinates: " << portraitData.eyeColorPaletteCoordinates.x << " " << portraitData.eyeColorPaletteCoordinates.y; // debug
		}
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