#include "Country.h"


Imperator::countryRankEnum Imperator::Country::getCountryRank() const
{
	if (provinceCount == 0) return countryRankEnum::migrantHorde;
	if (provinceCount == 1) return countryRankEnum::cityState;
	if (provinceCount <= 24) return countryRankEnum::localPower;
	if (provinceCount <= 99) return countryRankEnum::regionalPower;
	if (provinceCount <= 499) return countryRankEnum::majorPower;
	return countryRankEnum::greatPower;
}
