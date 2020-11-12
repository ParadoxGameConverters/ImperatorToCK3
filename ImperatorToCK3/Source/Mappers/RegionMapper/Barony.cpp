#include "Barony.h"
#include "ParserHelpers.h"

mappers::Barony::Barony(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::Barony::registerKeys()
{
	
	registerKeyword("province", [this](const std::string& provinceStr, std::istream& theStream) {
		provinceID = static_cast<unsigned int>(commonItems::singleInt(theStream).getInt());
	});
	registerKeyword(commonItems::catchallRegex, commonItems::ignoreItem);
}
