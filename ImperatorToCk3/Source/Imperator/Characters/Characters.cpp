#include "Characters.h"
#include "Character.h"
#include "../Families/Families.h"
#include "Log.h"
#include "ParserHelpers.h"

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
	registerRegex("[A-Za-z0-9\\_:.-]+", commonItems::ignoreItem);
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
	Log(LogLevel::Info) << "<> " << counter << " families linked.";
}

void ImperatorWorld::Characters::linkSpouses()
{
	auto counterLiege = 0;
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
	Log(LogLevel::Info) << "<> " << counterLiege << " lieges and " << counterSpouse << " spouses linked.";
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
	registerRegex("[A-Za-z0-9\\_:.-]+", commonItems::ignoreItem);
}