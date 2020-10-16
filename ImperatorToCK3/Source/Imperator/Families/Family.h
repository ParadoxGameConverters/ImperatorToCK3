#ifndef IMPERATOR_FAMILY_H
#define IMPERATOR_FAMILY_H
#include "Parser.h"

namespace Imperator
{
class Family: commonItems::parser
{
  public:
	Family(std::istream& theStream, unsigned long long theFamilyID);

	void updateFamily(std::istream& theStream);

	[[nodiscard]] const auto& getCulture() const { return culture; }
	[[nodiscard]] const auto& getPrestige() const { return prestige; }
	[[nodiscard]] const auto& getPrestigeRatio() const { return prestigeRatio; }
	[[nodiscard]] const auto& getKey() const { return key; }
	[[nodiscard]] const auto& getIsMinor() const { return isMinor; }

	[[nodiscard]] auto getID() const { return familyID; }

  private:
	void registerKeys();

	unsigned long long familyID = 0;
	std::string culture;
	double prestige = 0;
	double prestigeRatio = 0;
	std::string key;
	bool isMinor = false;
};
} // namespace Imperator

#endif // IMPERATOR_FAMILY_H