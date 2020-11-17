#ifndef IMPERATOR_REGION_H
#define IMPERATOR_REGION_H

#include "Parser.h"
#include "ImperatorArea.h"
#include "Log.h"

namespace mappers
{
class ImperatorRegion : commonItems::parser
{
  public:
	explicit ImperatorRegion(std::istream& theStream);

	[[nodiscard]] const auto& getAreas() const { return areas; }
	[[nodiscard]] bool regionContainsProvince(unsigned long long province) const;

	void linkArea(const std::string& areaName, const std::shared_ptr<ImperatorArea>& area) { areas[areaName] = area; }

  private:
	void registerKeys();
	std::map<std::string, std::shared_ptr<ImperatorArea>> areas;
};
} // namespace mappers

#endif // IMPERATOR_REGION_H