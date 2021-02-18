#ifndef IMPERATOR_COUNTRIES_H
#define IMPERATOR_COUNTRIES_H


#include "Parser.h"
#include "CountryFactory.h"


namespace Imperator
{
	class Country;
	class Families;
	class Countries: commonItems::parser
	{
	  public:
		Countries() = default;
		explicit Countries(std::istream& theStream);
		
		auto& operator= (const Countries& obj) { this->countries = obj.countries; return *this; }

		[[nodiscard]] const auto& getCountries() const { return countries; }

		void linkFamilies(const Families& theFamilies);

	  private:
		void registerKeys();

		Country::Factory countryFactory;

		std::map<unsigned long long, std::shared_ptr<Country>> countries;
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
} // namespace Imperator

#endif // IMPERATOR_COUNTRIES_H
