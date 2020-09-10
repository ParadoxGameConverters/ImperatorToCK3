#include "outTitle.h"

std::ostream& CK3::operator<<(std::ostream& output, const Title& title)
{
	output << title.titleName << " = {\n";
	output << "\tcolor " << title.color1.outputRgb() << "\n";
	output << "\tcolor2 " << title.color2.outputRgb() << "\n";
	const auto capital = title.capitalCounty;
	if (capital)
		output << "\tcapital = " << *capital << "\n";
	output << "}\n";
	
	return output;
}
