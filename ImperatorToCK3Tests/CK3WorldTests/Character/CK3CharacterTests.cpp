#include "CK3/Character/CK3Character.h"
#include "Mappers/CoaMapper/CoaMapper.h"
#include "gtest/gtest.h"
#include <sstream>



TEST(CK3World_CharacterTests, IDDefaultsTo0String) {
	const CK3::Character theCharacter;

	ASSERT_EQ("0", theCharacter.ID);
}

TEST(CK3World_CharacterTests, nameDefaultsToEmpty) {
	const CK3::Character theCharacter;

	ASSERT_TRUE(theCharacter.name.empty());
}