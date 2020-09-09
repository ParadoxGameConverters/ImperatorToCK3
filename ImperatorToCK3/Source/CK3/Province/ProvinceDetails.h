#ifndef CK3_PROVINCE_DETAILS_H
#define CK3_PROVINCE_DETAILS_H

#include "Parser.h"

namespace CK3
{
class ProvinceDetails: commonItems::parser
{
  public:
	ProvinceDetails() = default;
	explicit ProvinceDetails(const std::string& filePath);
	explicit ProvinceDetails(std::istream& theStream);
	void updateWith(const std::string& filePath);

	// These values are open to ease management.
	// This is a storage container for CK3::Province.
	// Some of these values are loaded from files, others are adjusted on the fly.
	//std::string owner;
	//std::string controller;
	std::string culture;
	std::string religion;

  private:
	void registerKeys();
};
} // namespace CK3

#endif // CK3_PROVINCE_DETAILS_H
