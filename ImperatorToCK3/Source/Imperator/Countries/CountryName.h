#ifndef IMPERATOR_COUNTRY_NAME_H
#define IMPERATOR_COUNTRY_NAME_H



#include "Mappers/LocalizationMapper/LocalizationMapper.h"
#include <string>
#include <optional>
#include <memory>



namespace Imperator {

class Country;
class CountryName {
public:
	CountryName() = default;
	CountryName(const CountryName& other);
	CountryName(CountryName&& other) noexcept;
	class Factory;
	
	CountryName& operator=(const CountryName& other);
	CountryName& operator=(CountryName&& other) noexcept;

	[[nodiscard]] const auto& getName() const { return name; }
	[[nodiscard]] std::string getAdjective() const;
	[[nodiscard]] const auto& getBase() const { return base; }

	[[nodiscard]] std::optional<mappers::LocBlock> getNameLocBlock(mappers::LocalizationMapper& localizationMapper, 
																   const std::map<unsigned long long, std::shared_ptr<Country>>& imperatorCountries) const;
	[[nodiscard]] std::optional<mappers::LocBlock> getAdjectiveLocBlock(mappers::LocalizationMapper& localizationMapper,
																		const std::map<unsigned long long, std::shared_ptr<Country>>& imperatorCountries) const;

private:
	std::string name;
	std::optional<std::string> adjective;
	std::unique_ptr<CountryName> base;
};

} // namespace Imperator



#endif // IMPERATOR_COUNTRY_NAME_H