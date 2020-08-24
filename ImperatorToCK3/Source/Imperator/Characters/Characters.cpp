#include "Characters.h"
#include "Character.h"
#include "../Families/Families.h"
#include "Log.h"
#include "ParserHelpers.h"
#include <base64.h>
#include <bitset>


long long binaryToDecimal(long long n)
{
	long long num = n;
	long long dec_value = 0;

	// Initializing base value to 1, i.e 2^0 
	int base = 1;

	long long temp = num;
	while (temp) {
		int last_digit = temp % 10;
		temp = temp / 10;

		dec_value += last_digit * base;

		base = base * 2;
	}

	return dec_value;
}


ImperatorWorld::Characters::Characters(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Characters::registerKeys()
{
	registerRegex("\\d+", [this](const std::string& charID, std::istream& theStream) {
		auto newCharacter = std::make_shared<Character>(theStream, std::stoi(charID));
		characters.insert(std::pair(newCharacter->getID(), newCharacter));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

void ImperatorWorld::Characters::linkFamilies(const Families& theFamilies)
{
	auto counter = 0;
	const auto& families = theFamilies.getFamilies();
	for (const auto& character: characters)
	{
		if (character.second->getFamily().first)
		{
			const auto& familyItr = families.find(character.second->getFamily().first);
			if (familyItr != families.end())
			{
				character.second->setFamily(familyItr->second);
				counter++;
			}
			else
			{
				Log(LogLevel::Warning) << "Family ID: " << character.second->getFamily().first << " has no definition!";
			}
		}
	}
	Log(LogLevel::Info) << "<> " << counter << " families linked to characters.";
}

void ImperatorWorld::Characters::linkSpouses()
{
	auto counterSpouse = 0;
	for (const auto& character: characters)
	{
		if (!character.second->getSpouses().empty())
		{
			std::map<int, std::shared_ptr<Character>> newSpouses;
			for (const auto& spouse: character.second->getSpouses())
			{
				const auto& characterItr = characters.find(spouse.first);
				if (characterItr != characters.end())
				{
					newSpouses.insert(std::pair(characterItr->first, characterItr->second));
					counterSpouse++;
				}
				else
				{
					Log(LogLevel::Warning) << "Spouse ID: " << spouse.first << " has no definition!";
				}
			}
			character.second->setSpouses(newSpouses);
		}
	}
	Log(LogLevel::Info) << "<> " << counterSpouse << " spouses linked.";
}


void ImperatorWorld::Characters::linkMothersAndFathers()
{
	auto counterMother = 0;
	auto counterFather = 0;
	for (const auto& character: characters)
	{
		if (character.second->getMother().first)
		{
			const auto& characterItr = characters.find(character.second->getMother().first);
			if (characterItr != characters.end())
			{
				character.second->setMother(std::pair(characterItr->first, characterItr->second));
				characterItr->second->registerChild(character);
				counterMother++;
			}
			else
			{
				Log(LogLevel::Warning) << "Mother ID: " << character.second->getMother().first << " has no definition!";
			}
		}

		if (character.second->getFather().first)
		{
			const auto& characterItr = characters.find(character.second->getFather().first);
			if (characterItr != characters.end())
			{
				character.second->setFather(std::pair(characterItr->first, characterItr->second));
				counterFather++;
				characterItr->second->registerChild(character);
			}
			else
			{
				Log(LogLevel::Warning) << "Father ID: " << character.second->getFather().first << " has no definition!";
			}
		}
	}
	Log(LogLevel::Info) << "<> " << counterMother << " mothers and " << counterFather << " fathers linked.";
}


void ImperatorWorld::Characters::extractPortraitDataFromDnaStrings()
{
	auto counter = 0;
	for (const auto& character : characters)
	{
		if (character.second->getDNA().size() == 552)
		{
			const std::string& hairStr = character.second->getDNA(); // .substr(0, 3);
			character.second->setdecodedhairstr(base64_decode(hairStr));
			//Log(LogLevel::Warning) << "Decoded hair string: " << character.second->getDecodedHairStr();

			//Log(LogLevel::Warning) << "Decoded string length: " << character.second->getDecodedHairStr().size();
			std::string binary_outputInformations;
			for (std::size_t i = 0; i < character.second->getDecodedHairStr().size(); ++i)
			{
				std::bitset<8> b(character.second->getDecodedHairStr().c_str()[i]);
				binary_outputInformations += b.to_string();
			}
			int x = binaryToDecimal(stoll(binary_outputInformations.substr(0, 18))) / 512;
			int y = binaryToDecimal(stoll(binary_outputInformations.substr(0, 18))) % 512;
			Log(LogLevel::Warning) << "Char ID "<< character.first << " has decoded hair in XY coordinates: " << x << " " << y ;
		}
	}
	Log(LogLevel::Info) << "<> Extracted portrait data from " << counter << " DNA strings.";
}




ImperatorWorld::CharactersBloc::CharactersBloc(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::CharactersBloc::registerKeys()
{
	registerKeyword("character_database", [this](const std::string& unused, std::istream& theStream) {
		characters = Characters(theStream);
		});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}