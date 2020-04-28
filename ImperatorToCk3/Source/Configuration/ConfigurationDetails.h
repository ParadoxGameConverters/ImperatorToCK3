#ifndef CONFIGURATION_DETAILS_H
#define CONFIGURATION_DETAILS_H

#include "Date.h"
#include "Parser.h"

class ConfigurationDetails: commonItems::parser
{
  public:
	ConfigurationDetails() = default;
	explicit ConfigurationDetails(std::istream& theStream);

	enum class IMPERATOR_DE_JURE { PROVS_AND_REGIONS = 1, COUNTRIES = 2, NO = 3 };

	std::string SaveGamePath;
	std::string ImperatorPath;
	std::string ImperatorModsPath;
	std::string Ck3Path;
	std::string outputName;

	IMPERATOR_DE_JURE imperatorDeJure = IMPERATOR_DE_JURE::NO;

  private:
	void registerKeys();
};

#endif // CONFIGURATION_DETAILS_H