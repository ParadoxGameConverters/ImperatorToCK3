#ifndef COA_MAPPER_H
#define COA_MAPPER_H



#include "Parser.h"
#include <map>
#include <optional>
#include <string>



class Configuration;

namespace mappers {

class CoaMapper: commonItems::parser {
  public:
	CoaMapper() = default;
	explicit CoaMapper(const Configuration& theConfiguration);
	explicit CoaMapper(const std::string& coaFilePath);

	[[nodiscard]] std::optional<std::string> getCoaForFlagName(const std::string& impFlagName);

  private:
	void registerKeys();

	std::map<std::string, std::string> coasMap;
};

} // namespace mappers



#endif // COA_MAPPER_H
