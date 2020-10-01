#include "outCharacter.h"
#include "Log.h"

std::ostream& CK3::operator<<(std::ostream& output, const Character& character)
{
	output << character.ID << " = {\n";
	if (!character.name.empty()) output << "\tname = \"" << character.name << "\"\n";
	if (character.female) output << "\tfemale = yes\n";
	if (!character.culture.empty()) output << "\tculture = " << character.culture << "\n";
	if (!character.religion.empty()) output << "\treligion = " << character.religion << "\n";

	//output father and mother
	if (character.father.second) output << "\tfather = " << character.father.first << "\n";
	if (character.mother.second) output << "\tmother = " << character.mother.first << "\n";
	// TODO: output add_spouse with earlier date if the pair has a born or unborn child
	for (const auto& [spouseID, spouseCharacter] : character.spouses)
	{
		output << "\t867.1.1 = { add_spouse = " << spouseID << " }\n";
	}

	
	for (const auto& trait : character.traits)
	{
		output << "\ttrait = " << trait << "\n";
	}

	output << "\t" << character.birthDate << " = { birth = yes }\n";
	if (character.deathDate) output << "\t" << *character.deathDate << " = { death = yes }\n";

	
	output << "}\n";
	
	return output;
}
