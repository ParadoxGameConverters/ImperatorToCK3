#ifndef CONFIGURATION_H
#define CONFIGURATION_H

#include "Date.h"
#include "Parser.h"

class Configuration: commonItems::parser
{
  public:
	Configuration();
	explicit Configuration(std::istream& theStream);

	enum class IMPERATOR_DE_JURE { PROVS_AND_REGIONS = 1, COUNTRIES = 2, NO = 3 };

	[[nodiscard]] const auto& getSaveGamePath() const { return SaveGamePath; }
	[[nodiscard]] const auto& getImperatorPath() const { return ImperatorPath; }
	[[nodiscard]] const auto& getImperatorModsPath() const { return ImperatorModsPath; }
	[[nodiscard]] const auto& getCK3Path() const { return CK3Path; }
	[[nodiscard]] const auto& getOutputName() const { return outputName; }
	[[nodiscard]] const auto& getImperatorDeJure() const { return imperatorDeJure; }

  private:
	void registerKeys();
	void setOutputName();
	void verifyImperatorPath() const;
	void verifyCK3Path() const;

	std::string SaveGamePath;
	std::string ImperatorPath;
	std::string ImperatorModsPath;
	std::string CK3Path;
	std::string outputName;

	IMPERATOR_DE_JURE imperatorDeJure = IMPERATOR_DE_JURE::NO;
};

#endif // CONFIGURATION_H