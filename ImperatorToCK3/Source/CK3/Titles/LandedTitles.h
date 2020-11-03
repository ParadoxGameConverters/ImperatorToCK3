#ifndef CK3_LANDEDTITLES_H
#define CK3_LANDEDTITLES_H

#include "Color.h"
#include "Parser.h"
#include <set>
extern commonItems::Color::Factory laFabricaDeColor;

namespace CK3
{
class Title;
class LandedTitles: commonItems::parser
{
  public:
	void loadTitles(const std::string& fileName);
	void loadTitles(std::istream& theStream);


	[[nodiscard]] std::optional<std::string> getCountyForProvince(unsigned long long provinceID);
	std::map<std::string, Title> foundTitles;			// title name, title

	

  private:
	  void registerKeys();

	
};
} // namespace CK3

#endif // CK3_LANDEDTITLES_H