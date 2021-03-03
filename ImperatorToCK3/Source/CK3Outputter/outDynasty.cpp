#include "outDynasty.h"
#include "CK3/Dynasties/Dynasty.h"



std::ostream& CK3::operator<<(std::ostream& output, const Dynasty& dynasty) {
	// output ID, name and culture
	output << dynasty.ID << " = {\n";
	output << "\tname = \"" << dynasty.name << "\"\n";
	output << "\tculture = " << dynasty.culture << "\n";
	output << "}\n";
	
	return output;
}
