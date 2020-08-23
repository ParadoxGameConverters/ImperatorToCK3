#ifndef IMPERATOR_COUNTRIES_H
#define IMPERATOR_COUNTRIES_H
#include "Parser.h"

namespace ImperatorWorld
{
	class Country;
	class Families;
	class Countries: commonItems::parser
	{
	  public:
		Countries() = default;
		Countries(std::istream& theStream);

		[[nodiscard]] const auto& getCountries() const { return countries; }

		void linkFamilies(const Families& theFamilies);

	  private:
		void registerKeys();

		std::map<int, std::shared_ptr<Country>> countries;
	};

	class CountriesBloc : commonItems::parser
	{
	public:
		CountriesBloc() = default;
		explicit CountriesBloc(std::istream& theStream);

		[[nodiscard]] const auto& getCountriesFromBloc() const { return countries; }

	private:
		void registerKeys();

		Countries countries;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_COUNTRIES_H
