#ifndef IMPERATOR_COUNTRY_H
#define IMPERATOR_COUNTRY_H



#include "Parser.h"
#include "Color.h"
#include <set>



namespace CK3 {
class Title;
} // namespace CK3

namespace Imperator {

typedef struct CurrenciesStruct {
	double manpower = 0;
	double gold = 0;
	double stability = 50;
	double tyranny = 0;
	double war_exhaustion = 0;
	double aggressive_expansion = 0;
	double political_influence = 0;
	double military_experience = 0;
} CurrenciesStruct;

enum class countryTypeEnum { rebels, pirates, barbarians, mercenaries, real };
enum class countryRankEnum { migrantHorde, cityState, localPower, regionalPower, majorPower, greatPower };

class Family;
class Province;
class Country {
public:
	class Factory;
	Country() = default;

	enum class GovernmentType { monarchy, republic, tribal };

	[[nodiscard]] auto getID() const { return ID; }
	[[nodiscard]] auto getMonarch() const { return monarch; }
	[[nodiscard]] const std::string& getTag() const { return tag; }
	[[nodiscard]] const auto& getName() const { return name; }
	[[nodiscard]] const auto& getFlag() const { return flag; }
	[[nodiscard]] const auto& getCountryType() const { return countryType; }
	[[nodiscard]] const auto& getCapital() const { return capital; }
	[[nodiscard]] const auto& getGovernment() const { return government; }
	[[nodiscard]] const std::set<std::string>& getLaws() const;
	[[nodiscard]] const auto& getCurrencies() const { return currencies; }
	[[nodiscard]] const auto& getColor1() const { return color1; }
	[[nodiscard]] const auto& getColor2() const { return color2; }
	[[nodiscard]] const auto& getColor3() const { return color3; }
	[[nodiscard]] const auto& getFamilies() const { return families; }
	[[nodiscard]] const auto& getCK3Title() const { return ck3Title; }

	[[nodiscard]] countryRankEnum getCountryRank() const;

	void setFamilies(const std::map<unsigned long long, std::shared_ptr<Family>>& newFamilies) { families = newFamilies; }

	void registerProvince(const std::shared_ptr<Province>& province) { provinces.insert(province); ++provinceCount; }
	void setCK3Title(const std::shared_ptr<CK3::Title>& theTitle) { ck3Title = theTitle; }

private:
	uint64_t ID = 0;
	std::optional<unsigned long long> monarch; // >=0 are valid
	std::string tag;
	std::string name;
	std::string flag;
	countryTypeEnum countryType = countryTypeEnum::real;
	std::optional<unsigned long long> capital;
	std::optional<std::string> government;
	GovernmentType governmentType = GovernmentType::monarchy;
	std::set<std::string> monarchyLaws;
	std::set<std::string> republicLaws;
	std::set<std::string> tribalLaws;

	std::optional<commonItems::Color> color1;
	std::optional<commonItems::Color> color2;
	std::optional<commonItems::Color> color3;
	CurrenciesStruct currencies;

	std::map<unsigned long long, std::shared_ptr<Family>> families;
	
	std::set<std::shared_ptr<Province>> provinces;
	unsigned int provinceCount = 0; // used to determine country rank

	std::shared_ptr<CK3::Title> ck3Title;
};

} // namespace Imperator



#endif // IMPERATOR_COUNTRY_H
