#ifndef CK3_LANDEDTITLES_H
#define CK3_LANDEDTITLES_H

#include "Color.h"
#include "Parser.h"
#include <set>
extern commonItems::Color::Factory laFabricaDeColor;

namespace CK3
{
class Title;
class BaronyHolding;
class LandedTitles: commonItems::parser
{
  public:
	void loadTitles(std::istream& theStream);
	void loadTitles(const std::string& fileName);

	[[nodiscard]] auto isDefiniteForm() const { return definiteForm; }
	[[nodiscard]] auto isLandless() const { return landless; }
	[[nodiscard]] const auto& getColor() const { return color; }
	[[nodiscard]] const auto& getCapital() const { return capital; }
	[[nodiscard]] const auto& getProvince() const { return province; }
	[[nodiscard]] const auto& getFoundTitles() const { return foundTitles; }

	[[nodiscard]] std::optional<std::string> getCountyForProvince(int provinceID);

	std::optional<int> capitalBarony;	// Capital barony (for counties)

  private:
	void registerKeys();

	bool definiteForm = false;
	bool landless = false;
	std::optional<commonItems::Color> color;
	std::pair<std::string, std::shared_ptr<Title>> capital;	// Capital county
	std::optional<int> province; // province is area on map. b_ barony is its corresponding title.
	std::set<int> countyProvinces;
	std::map<std::string, LandedTitles> foundTitles;			// title name, title
};
} // namespace CK3

#endif // CK3_LANDEDTITLES_H