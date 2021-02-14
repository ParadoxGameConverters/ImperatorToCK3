#ifndef CK3_PROVINCE_DETAILS
#define CK3_PROVINCE_DETAILS

#include "ConvenientParser.h"

namespace CK3
{
class ProvinceDetails: commonItems::convenientParser
{
  public:
	ProvinceDetails() = default;
	explicit ProvinceDetails(const std::string& filePath);
	explicit ProvinceDetails(std::istream& theStream);
	void updateWith(const std::string& filePath);

	// These values are open to ease management.
	// This is a storage container for CK3::Province.
	std::string culture;
	std::string religion;
	std::string holding = "none";

  private:
	void registerKeys();
};
} // namespace CK3

#endif // CK3_PROVINCE_DETAILS
