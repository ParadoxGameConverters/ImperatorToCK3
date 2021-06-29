#ifndef IMPERATOR_MOD_H
#define IMPERATOR_MOD_H



#include "ConvenientParser.h"



namespace Imperator {

class Mod: commonItems::convenientParser {
  public:
	explicit Mod(std::istream& theStream);

	[[nodiscard]] const auto& getName() const { return name; }
	[[nodiscard]] const auto& getPath() const { return path; }
	[[nodiscard]] auto looksValid() const { return !name.empty() && !path.empty(); }
	[[nodiscard]] auto isCompressed() const { return compressed; }

	void setPath(const std::string& thePath) { path = thePath; }

  private:
	bool compressed = false;

	std::string name;
	std::string path;
};

}  // namespace Imperator



#endif	// IMPERATOR_MOD_H