#ifndef CK3_DUCHY_H
#define CK3_DUCHY_H

#include "Parser.h"
#include "County.h"

namespace mappers
{
class Duchy: commonItems::parser
{
  public:
	explicit Duchy(std::istream& theStream);

	[[nodiscard]] const auto& getCounties() const { return counties; }
	[[nodiscard]] bool duchyContainsProvince(int province) const;

  private:
	void registerKeys();
	std::map<std::string, County> counties;
};
} // namespace mappers

#endif // CK3_DUCHY_H