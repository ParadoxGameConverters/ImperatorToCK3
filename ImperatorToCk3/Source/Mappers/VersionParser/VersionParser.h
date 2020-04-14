#ifndef VERSION_PARSER_H
#define VERSION_PARSER_H

#include "newParser.h"

namespace mappers
{
class VersionParser: commonItems::parser
{
  public:
	VersionParser();
	explicit VersionParser(std::istream& theStream);
	[[nodiscard]] const auto& getName() const { return name; }
	[[nodiscard]] const auto& getVersion() const { return version; }
	[[nodiscard]] const auto& getDescription() const { return descriptionLine; }

	friend std::ostream& operator<<(std::ostream& output, const VersionParser& versionParser);

  private:
	void registerKeys();

	std::string name;
	std::string version;
	std::string descriptionLine;
};
} // namespace mappers

#endif // VERSION_PARSER_H