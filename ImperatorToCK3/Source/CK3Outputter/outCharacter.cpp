#include "outCharacter.h"
#include "Log.h"

std::ostream& CK3::operator<<(std::ostream& output, const Character& character)
{
	output << character.ID << " = {\n";
	if (character.female) output << "\tfemale = yes\n";
	if (!character.name.empty()) output << "\tname = \"" << character.name << "\"\n";
	if (!character.culture.empty()) output << "\tculture = " << character.culture << "\n";
	if (!character.religion.empty()) output << "\treligion = " << character.religion << "\n";
	output << "\t" << character.birthDate << " = { birth = yes }\n";
	output << "}\n";
	
	return output;
}
