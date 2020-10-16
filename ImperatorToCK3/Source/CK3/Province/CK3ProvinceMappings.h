#ifndef CK3_PROVINCE_MAPPINGS_H
#define CK3_PROVINCE_MAPPINGS_H

#include "Parser.h"

namespace CK3
{
	class ProvinceMappings : commonItems::parser
	{
		/*
		
		This class is used to read game/history/province_mapping in CK3.
		CK3 uses province_mapping to set history for provinces that don't need unique entries.


		Example unique entry in game/history/provinces:
		
		6872 = {
			religion = coptic
			culture = coptic
		}


		Example province_mapping in game/history/province_mapping:

		6874 = 6872


		Now 6874 history is same as 6872 history.
		
		*/
	  public:
		ProvinceMappings() = default;
		explicit ProvinceMappings(const std::string& theFile);
		[[nodiscard]] const auto& getMappings() const { return mappings; }

	  private:
		void registerKeys();

		std::map<unsigned long long, unsigned long long> mappings;
	};
} // namespace CK3

#endif // CK3_PROVINCE_MAPPINGS_H
