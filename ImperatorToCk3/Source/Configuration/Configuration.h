#ifndef CONFIGURATION_H
#define CONFIGURATION_H

#include "ConfigurationDetails.h"
#include "Date.h"
#include "Parser.h"

class Configuration: commonItems::parser
{
  public:
	Configuration();
	explicit Configuration(std::istream& theStream);

	[[nodiscard]] const auto& getSaveGamePath() const { return details.SaveGamePath; }
	[[nodiscard]] const auto& getImperatorPath() const { return details.ImperatorPath; }
	[[nodiscard]] const auto& getImperatorModsPath() const { return details.ImperatorModsPath; }
	[[nodiscard]] const auto& getCk3Path() const { return details.Ck3Path; }
	[[nodiscard]] const auto& getOutputName() const { return details.outputName; }
	[[nodiscard]] const auto& getImperatorDeJure() const { return details.imperatorDeJure; }

  private:
	void registerKeys();
	void setOutputName();
	void verifyImperatorPath() const;
	void verifyCk3Path() const;

	ConfigurationDetails details;
};

#endif // CONFIGURATION_H