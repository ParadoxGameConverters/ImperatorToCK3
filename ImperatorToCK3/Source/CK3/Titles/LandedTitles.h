#ifndef CK3_LANDEDTITLES_H
#define CK3_LANDEDTITLES_H

#include "Parser.h"

namespace CK3
{
class Title;
class LandedTitles: commonItems::parser
{
  public:
	void loadTitles(const std::string& fileName);
	void loadTitles(std::istream& theStream);

	void insertTitle(const std::shared_ptr<Title>& title);
	void eraseTitle(const std::string& name);

	[[nodiscard]] const auto& getTitles() const { return foundTitles; }
	[[nodiscard]] std::optional<std::string> getCountyForProvince(unsigned long long provinceID);

  private:
	void registerKeys();
	
	std::map<std::string, std::shared_ptr<Title>> foundTitles;			// title name, title
};
} // namespace CK3

#endif // CK3_LANDEDTITLES_H