#include "outProvince.h"
#include "CK3/Province/CK3Province.h"



std::ostream& CK3::operator<<(std::ostream& output, const Province& province) {
	output << province.getID() << " = {\n";
	output << "\t" << "867.1.1 = {\n"; // temporary workaround for replace_path in .mod not working #TODO(#33): remove when replace_path works
	if (!province.details.culture.empty()) {
		output << "\t\t" << "culture = " << province.details.culture << "\n";
	}
	if (!province.details.religion.empty()) {
		output << "\t\t" << "religion = " << province.details.religion << "\n";
	}
	output << "\t\t" << "holding = " << province.details.holding << "\n";
	if (!province.details.buildings.empty()) {
		output << "\t\t" << "buildings = {\n";
		for (const auto& building : province.details.buildings) {
			output << "\t\t\t" << building << "\n";
		}
		output << "\t\t" << "}\n";
	}
	output << "\t" << "}\n"; // temporary workaround for replace_path in .mod not working #TODO(#33): remove when replace_path works
	output << "}\n";
	return output;
}
