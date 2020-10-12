#ifndef IMPERATOR_COUNTRY_H
#define IMPERATOR_COUNTRY_H

#include <set>
#include "Parser.h"
#include "Color.h"

namespace CK3
{
	class Title;
} // namespace CK3
namespace Imperator
{
	typedef struct CurrenciesStruct
	{
		int manpower = 0;
		int gold = 0;
		int stability = 50;
		int tyranny = 0;
		int war_exhaustion = 0;
		int aggressive_expansion = 0;
		int political_influence = 0;
		int military_experience = 0;
	} CurrenciesStruct;

	enum class countryTypeEnum { rebels, pirates, barbarians, mercenaries, real };
	enum class countryRankEnum { migrantHorde, cityState, localPower, regionalPower, majorPower, greatPower };

	class Family;
	class Province;
	class Country: commonItems::parser
	{
		public:
			Country(std::istream& theStream, int countryID);

			[[nodiscard]] const std::string& getTag() const { return tag; }
			[[nodiscard]] const auto& getName() const { return name; }
			[[nodiscard]] const auto& getFlag() const { return flag; }
			[[nodiscard]] const auto& getCountryType() const { return countryType; }
			[[nodiscard]] const auto& getCapital() const { return capital; }
			[[nodiscard]] const auto& getCurrencies() const { return currencies; }
			[[nodiscard]] const auto& getColor1() const { return color1; }
			[[nodiscard]] const auto& getColor2() const { return color2; }
			[[nodiscard]] const auto& getColor3() const { return color3; }
			[[nodiscard]] const auto& getFamilies() const { return families; }
			[[nodiscard]] auto getID() const { return countryID; }
			[[nodiscard]] auto getMonarch() const { return monarch; }

			[[nodiscard]] countryRankEnum getCountryRank() const;

			void setFamilies(const std::map<int, std::shared_ptr<Family>>& newFamilies) { families = newFamilies; }

			void registerProvince(const std::shared_ptr<Province>& province) { provinces.insert(province); ++provinceCount; }
			void registerCK3Title(const std::shared_ptr<CK3::Title>& theTitle) { ck3Title = theTitle; }

		private:
			void registerKeys();

			int countryID = 0;
			std::optional<unsigned int> monarch; // >=0 are valid
			std::string tag;
			std::string name;
			std::string flag;
			countryTypeEnum countryType = countryTypeEnum::real;
			std::optional<int> capital;
	
			std::optional<commonItems::Color> color1;
			std::optional<commonItems::Color> color2;
			std::optional<commonItems::Color> color3;
			CurrenciesStruct currencies;

			std::map<int, std::shared_ptr<Family>> families;
		
			std::set<std::shared_ptr<Province>> provinces;
			unsigned int provinceCount = 0; // used to determine country rank

		
			std::shared_ptr<CK3::Title> ck3Title;
	};
} // namespace Imperator

#endif // IMPERATOR_COUNTRY_H
