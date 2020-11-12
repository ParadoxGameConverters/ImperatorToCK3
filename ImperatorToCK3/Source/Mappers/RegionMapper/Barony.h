#ifndef CK3_BARONY_H
#define CK3_BARONY_H

#include "Parser.h"

namespace mappers
{
class Barony: commonItems::parser
{
  public:
	explicit Barony(std::istream& theStream);

	[[nodiscard]] const auto& getProvinceID() const { return provinceID; }

  private:
	void registerKeys();
	std::optional<unsigned int> provinceID;
};
} // namespace mappers

#endif // CK3_BARONY_H