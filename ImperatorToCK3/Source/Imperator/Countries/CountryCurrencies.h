#ifndef IMPERATOR_COUNTRY_CURRENCIES_H
#define IMPERATOR_COUNTRY_CURRENCIES_H
#include "ConvenientParser.h"

namespace Imperator
{
class CountryCurrencies : commonItems::convenientParser
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

	double manpower = 0;
	double gold = 0;
	double stability = 50;
	double tyranny = 0;
	double war_exhaustion = 0;
	double aggressive_expansion = 0;
	double political_influence = 0;
	double military_experience = 0;
};
} // namespace Imperator

#endif // IMPERATOR_COUNTRY_CURRENCIES_H