#include "outTitle.h"
#include "Log.h"

std::ostream& CK3::operator<<(std::ostream& output, const Title& title)
{
	output << title.titleName << " = {\n";
	if (title.color1) output << "\tcolor " << *title.color1 << "\n";
	else Log(LogLevel::Warning) << "Title " << title.titleName << " has no color.";
	if (title.color2) output << "\tcolor2 " << *title.color2 << "\n";
	else Log(LogLevel::Warning) << "Title " << title.titleName << " has no color2.";
	const auto capital = title.capitalCounty;
	if (capital)
		output << "\tcapital = " << *capital << "\n";
	output << "}\n";
	
	return output;
}
