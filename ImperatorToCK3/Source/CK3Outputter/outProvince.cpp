#include "outProvince.h"

std::ostream& CK3::operator<<(std::ostream& output, const Province& province)
{
	output << province.getProvinceID() << " = {\n";
	output << "\t867.1.1 = {\n"; // temporary workaround for replace_path in .mod not working #TODO: remove when replace_path works
	if (!province.details.culture.empty())
		output << "\tculture = " << province.details.culture << "\n";
	if (!province.details.religion.empty())
		output << "\treligion = " << province.details.religion << "\n";
	output << "\t}\n"; // temporary workaround for replace_path in .mod not working #TODO: remove when replace_path works
	output << "}\n";
	return output;
}
