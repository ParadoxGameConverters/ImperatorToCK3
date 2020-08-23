#ifndef IMPERATOR_COUNTRY_H
#define IMPERATOR_COUNTRY_H
#include "Date.h"
#include "Parser.h"

namespace ImperatorWorld
{

struct CurrenciesStruct
{
	int manpower = 0;
	int gold = 0;
	int stability = 50;
	int tyranny = 0;
	int war_exhaustion = 0;
	int aggressive_expansion = 0;
	int political_influence = 0;
	int military_experience = 0;
};

class Country: commonItems::parser
{
  public:
	Country(std::istream& theStream, int cntrID);

	[[nodiscard]] const std::string& getTag() const { return tag; }
	[[nodiscard]] const auto& getName() const { return name; }
	[[nodiscard]] const auto& getCurrencies() const { return currencies; }

	[[nodiscard]] auto getWealth() const { return wealth; }
	[[nodiscard]] auto getID() const { return countryID; }

  private:
	void registerKeys();

	int countryID = 0;
	double wealth = 0;
	std::string tag;
	std::string name;
	CurrenciesStruct currencies;
};
} // namespace ImperatorWorld

#endif // IMPERATOR_COUNTRY_H
