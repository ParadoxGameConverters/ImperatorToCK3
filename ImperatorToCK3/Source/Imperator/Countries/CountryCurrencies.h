#ifndef IMPERATOR_COUNTRY_CURRENCIES_H
#define IMPERATOR_COUNTRY_CURRENCIES_H
#include "Parser.h"

namespace ImperatorWorld
{
class CountryCurrencies : commonItems::parser
{
  public:
	CountryCurrencies() = default;
	explicit CountryCurrencies(std::istream& theStream);

	[[nodiscard]] const auto& getManpower() const { return manpower; }
	[[nodiscard]] const auto& getGold() const { return gold; }
	[[nodiscard]] const auto& getStability() const { return stability; }
	[[nodiscard]] const auto& getTyranny() const { return tyranny; }
	[[nodiscard]] const auto& getWarExhaustion() const { return war_exhaustion; }
	[[nodiscard]] const auto& getAggressiveExpansion() const { return aggressive_expansion; }
	[[nodiscard]] const auto& getPoliticalInfluence() const { return political_influence; }
	[[nodiscard]] const auto& getMilitaryExperience() const { return military_experience; }

  private:
	void registerKeys();

	int manpower = 0;
	int gold = 0;
	int stability = 50;
	int tyranny = 0;
	int war_exhaustion = 0;
	int aggressive_expansion = 0;
	int political_influence = 0;
	int military_experience = 0;
};
} // namespace ImperatorWorld

#endif // IMPERATOR_COUNTRY_CURRENCIES_H