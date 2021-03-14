#ifndef CK3_PROVINCE_DETAILS_H
#define CK3_PROVINCE_DETAILS_H



#include "CommonUtilities/HistoryFactory.h"



namespace CK3 {

class ProvinceDetails {
  public:
	ProvinceDetails() = default;
	explicit ProvinceDetails(std::istream& theStream);

	// These values are open to ease management.
	// This is a storage container for CK3::Province.
	std::string culture;
	std::string religion;
	std::string holding = "none";

  private:
	static History::Factory historyFactory;
};

} // namespace CK3



#endif // CK3_PROVINCE_DETAILS_H
