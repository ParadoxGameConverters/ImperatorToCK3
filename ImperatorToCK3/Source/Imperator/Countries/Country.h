#ifndef IMPERATOR_COUNTRY_H
#define IMPERATOR_COUNTRY_H

#include "Parser.h"
#include "Color.h"

namespace CK3
{
	class Title;
} // namespace CK3
namespace ImperatorWorld
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

	class Family;
	class Country: commonItems::parser
	{
		public:
			Country(std::istream& theStream, int cntrID);

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

			void setFamilies(const std::map<int, std::shared_ptr<Family>>& newFamilies) { families = newFamilies; }

			void registerCK3Title(const std::pair<std::string, std::shared_ptr<CK3::Title>>& theTitle) { ck3Title = theTitle; }

		private:
			void registerKeys();

			int countryID = 0;
			std::optional<unsigned int> monarch; // >=0 are valid
			std::string tag;
			std::string name;
			std::string flag;
			std::string countryType;
			std::optional<int> capital;
	
			std::optional<commonItems::Color> color1;
			std::optional<commonItems::Color> color2;
			std::optional<commonItems::Color> color3;
			CurrenciesStruct currencies;

			std::map<int, std::shared_ptr<Family>> families;
			std::pair<std::string, std::shared_ptr<CK3::Title>> ck3Title;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_COUNTRY_H
