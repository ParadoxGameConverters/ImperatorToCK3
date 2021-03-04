#ifndef IMPERATOR_PROVINCE_NAME_H
#define IMPERATOR_PROVINCE_NAME_H



#include "Parser.h"



namespace Imperator {

class ProvinceName: commonItems::parser {
  public:
	ProvinceName() = default;
	explicit ProvinceName(std::istream& theStream);

	[[nodiscard]] const auto& getName() const { return name; }

  private:
	void registerKeys();

	std::string name;
};

} // namespace Imperator

#endif // IMPERATOR_PROVINCE_NAME_H