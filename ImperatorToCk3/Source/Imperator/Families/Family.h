#ifndef IMPERATOR_FAMILY_H
#define IMPERATOR_FAMILY_H
#include "newParser.h"

namespace ImperatorWorld
{
class Family: commonItems::parser
{
  public:
	Family(std::istream& theStream, int theFamilyID);

	void updateFamily(std::istream& theStream);

	[[nodiscard]] const auto& getCulture() const { return culture; }
	[[nodiscard]] const auto& getPrestige() const { return prestige; }
	[[nodiscard]] const auto& getPrestigeRatio() const { return prestigeRatio; }
	[[nodiscard]] const auto& getKey() const { return key; }

	[[nodiscard]] auto getID() const { return familyID; }

  private:
	void registerKeys();

	int familyID = 0;
	int owner = 0;
	std::string culture;
	double prestige;
	double prestigeRatio;
	std::string key;
};
} // namespace ImperatorWorld

#endif // IMPERATOR_FAMILY_H