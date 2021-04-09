#include "outCharacter.h"
#include "CK3/Character/CK3Character.h"



std::ostream& CK3::operator<<(std::ostream& output, const Character& character) {
	// output ID, name, sex, culture, religion
	output << character.ID << " = {\n";
	if (!character.name.empty())
		output << "\t" << "name = \"" << character.name << "\"\n";
	if (character.female)
		output << "\t" << "female = yes\n";
	if (!character.culture.empty())
		output << "\t" << "culture = " << character.culture << "\n";
	if (!character.religion.empty())
		output << "\t" << "religion = " << character.religion << "\n";

	// output dynasty
	if (character.dynastyID)
		output << "\t" << "dynasty = " << *character.dynastyID << "\n";

	//output father and mother
	if (character.father.second)
		output << "\t" << "father = " << character.father.first << "\n";
	if (character.mother.second)
		output << "\t" << "mother = " << character.mother.first << "\n";
	
	// output spouse
	// TODO: output add_spouse with earlier date if the pair has a born or unborn child
	for (const auto& [spouseID, spouseCharacter] : character.spouses) {
		output << "\t" << "867.1.1 = { add_spouse = " << spouseID << " }\n";
	}

	// output nickname
	if (!character.nickname.empty()) {
		date nicknameDate = date{ 867,1,1 };
		if (character.deathDate) {
			nicknameDate = *character.deathDate;
		}
		output << "\t" << nicknameDate << " = { give_nickname = " << character.nickname << " }\n";
	}

	// output traits
	for (const auto& trait : character.traits) {
		output << "\t" << "trait = " << trait << "\n";
	}

	// output birthdate and deathdate
	output << "\t" << character.birthDate << " = { birth = yes }\n";
	if (character.deathDate) {
		output << "\t" << *character.deathDate << " = {\n";
		output << "\t\t" << "death = ";
		if (character.deathReason) {
			output << "{ death_reason = " << *character.deathReason << " }\n";
		}
		else output << "yes\n";
		output << "\t}\n";
	}
	
	output << "}\n";
	
	return output;
}
