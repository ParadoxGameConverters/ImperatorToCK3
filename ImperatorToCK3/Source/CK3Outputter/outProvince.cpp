#include "outProvince.h"
#include "CK3/Province/CK3Province.h"



std::ostream& CK3::operator<<(std::ostream& output, const Province& province) {
	output << province.getID() << " = {\n";
	if (!province.details.culture.empty()) {
		output << "\t" << "culture = " << province.details.culture << "\n";
	}
	if (!province.details.religion.empty()) {
		output << "\t" << "religion = " << province.details.religion << "\n";
	}
	output << "\t" << "holding = " << province.details.holding << "\n";
	if (!province.details.buildings.empty()) {
		output << "\t" << "buildings = {\n";
		for (const auto& building : province.details.buildings) {
			output << "\t\t" << building << "\n";
		}
		output << "\t" << "}\n";
	}
	output << "}\n";
	return output;
}
