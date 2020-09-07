#ifndef CK3_PROVINCE_MAPPINGS_H
#define CK3_PROVINCE_MAPPINGS_H

#include "Parser.h"

namespace CK3
{
	class ProvinceMappings : commonItems::parser
	{
	  public:
		ProvinceMappings() = default;
		explicit ProvinceMappings(const std::string& theFile);
		[[nodiscard]] const auto& getMappings() const { return mappings; }

	  private:
		void registerKeys();

		std::map<int, int> mappings;
	};
} // namespace CK3

#endif // CK3_PROVINCE_MAPPINGS_H
