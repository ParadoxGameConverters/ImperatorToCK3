#ifndef IMPERATOR_FAMILY_H
#define IMPERATOR_FAMILY_H


#include <string>


namespace Imperator
{
class Family
{
  public:
	class Factory;
	Family() = default;

	[[nodiscard]] auto getID() const { return ID; }
	[[nodiscard]] const auto& getKey() const { return key; }
	[[nodiscard]] const auto& getCulture() const { return culture; }
	[[nodiscard]] const auto& getPrestige() const { return prestige; }
	[[nodiscard]] const auto& getPrestigeRatio() const { return prestigeRatio; }
	[[nodiscard]] const auto& getMembers() const { return members; }
	[[nodiscard]] const auto& isMinor() const { return minor; }


  private:
	unsigned long long ID = 0;
	std::string key;
	std::string culture;
	double prestige = 0;
	double prestigeRatio = 0;
	std::vector<unsigned long long> members;
	bool minor = false;
};
} // namespace Imperator

#endif // IMPERATOR_FAMILY_H