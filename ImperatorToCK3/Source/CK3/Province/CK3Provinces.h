#ifndef CK3_PROVINCES_H
#define CK3_PROVINCES_H



#include "CK3Province.h"
#include "Parser.h"



namespace CK3 {

class Provinces: commonItems::parser {
	public:
	Provinces() = default;
	explicit Provinces(const std::string& filePath);
	[[nodiscard]] const auto& getProvinces() const { return provinces; }

	private:
	void registerKeys();

	std::map<unsigned long long, std::shared_ptr<Province>> provinces;
};

} // namespace CK3



#endif // CK3_PROVINCES_H
