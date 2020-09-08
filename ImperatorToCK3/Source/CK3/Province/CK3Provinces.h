#ifndef CK3_PROVINCES_H
#define CK3_PROVINCES_H
#include "Parser.h"
#include "CK3Province.h"

namespace CK3
{
	class Provinces: commonItems::parser
	{
	  public:
		Provinces() = default;
		explicit Provinces(const std::string& filePath);
		[[nodiscard]] const auto& getProvinces() const { return provinces; }

	  private:
		void registerKeys();

		std::map<int, std::shared_ptr<Province>> provinces;
	};
} // namespace CK3

#endif // CK3_PROVINCES_H
