#include "CK3/Dynasties/Dynasty.h"
#include "Imperator/Families/FamilyFactory.h"
#include "Mappers/LocalizationMapper/LocalizationMapper.h"
#include "gtest/gtest.h"
#include <sstream>



TEST(CK3World_DynastyTests, idIsProperlyConverted) {
	std::stringstream familyStream;
	const auto family = Imperator::Family::Factory().getFamily(familyStream, 45);
	
	const mappers::LocalizationMapper locMapper;
	const CK3::Dynasty dynasty{ *family, locMapper };

	ASSERT_EQ("dynn_IMPTOCK3_45", dynasty.getID());
}

TEST(CK3World_DynastyTests, nameIsProperlyConverted) {
	std::stringstream familyStream;
	const auto family = Imperator::Family::Factory().getFamily(familyStream, 45);
	
	const mappers::LocalizationMapper locMapper;
	const CK3::Dynasty dynasty{ *family, locMapper };

	ASSERT_EQ("dynn_IMPTOCK3_45", dynasty.getName());
}
