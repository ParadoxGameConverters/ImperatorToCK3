#ifndef CONFIGURATION_H
#define CONFIGURATION_H



#include "ConverterVersion.h"
#include "Parser.h"


class Configuration: commonItems::parser {
  public:
	Configuration(const commonItems::ConverterVersion& converterVersion);
	explicit Configuration(std::istream& theStream);

	enum class IMPERATOR_DE_JURE { REGIONS = 1, COUNTRIES = 2, NO = 3 };

	[[nodiscard]] const auto& getSaveGamePath() const { return SaveGamePath; }
	[[nodiscard]] const auto& getImperatorPath() const { return ImperatorPath; }
	[[nodiscard]] const auto& getImperatorDocsPath() const { return ImperatorDocsPath; }
	[[nodiscard]] const auto& getCK3Path() const { return CK3Path; }
	[[nodiscard]] const auto& getCK3ModsPath() const { return CK3ModsPath; }
	[[nodiscard]] const auto& getOutputModName() const { return outputModName; }
	[[nodiscard]] const auto& getImperatorDeJure() const { return imperatorDeJure; }
	[[nodiscard]] const auto& getConvertBirthAndDeathDates() const { return convertBirthAndDeathDates; }

  private:
	void registerKeys();
	void setOutputName();
	void verifyImperatorPath() const;
	void verifyCK3Path() const;
	void verifyImperatorVersion(const commonItems::ConverterVersion& converterVersion) const;
	void verifyCK3Version(const commonItems::ConverterVersion& converterVersion) const;

	std::string SaveGamePath;
	std::string ImperatorPath;
	std::string ImperatorDocsPath;
	std::string CK3Path;
	std::string CK3ModsPath;
	std::string outputModName;

	IMPERATOR_DE_JURE imperatorDeJure = IMPERATOR_DE_JURE::NO;
	bool convertBirthAndDeathDates = true;
};



#endif	// CONFIGURATION_H