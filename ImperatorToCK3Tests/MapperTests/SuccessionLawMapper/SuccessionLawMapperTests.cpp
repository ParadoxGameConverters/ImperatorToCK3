#include "Mappers/SuccessionLawMapper/SuccessionLawMapper.h"
#include "gtest/gtest.h"
#include <sstream>



TEST(Mappers_SuccessionLawMapperTests, nonMatchGivesEmptySet)
{
	std::stringstream input;
	input << "link = { ck3 = ck3law imp = implaw }";
	const mappers::SuccessionLawMapper mapper(input);

	const auto& ck3Law = mapper.getCK3LawsForImperatorLaws({ "madeUpLaw" });
	ASSERT_TRUE(ck3Law.empty());
}


TEST(Mappers_SuccessionLawMapperTests, ck3LawCanBeFound)
{
	std::stringstream input;
	input << "link = { ck3 = ck3law imp = implaw }";
	const mappers::SuccessionLawMapper mapper(input);

	const auto& ck3Laws = mapper.getCK3LawsForImperatorLaws({ "implaw" });
	ASSERT_EQ(std::set<std::string>{"ck3law"}, ck3Laws);
}


TEST(Mappers_SuccessionLawMapperTests, multipleLawsCanBeReturned)
{
	std::stringstream input;
	input << "link = { imp = implaw ck3 = ck3law ck3 = ck3law2 }\n";
	input << "link = { imp = implaw ck3 = ck3law3 }\n";
	input << "link = { imp = implaw2 ck3 = ck3law4 }\n";
	input << "link = { imp = implaw3 ck3 = ck3law5 }\n";
	const mappers::SuccessionLawMapper mapper(input);

	const auto& ck3Laws = mapper.getCK3LawsForImperatorLaws({ "implaw", "implaw3" });
	std::set<std::string> expectedReturnedLaws{ "ck3law", "ck3law2", "ck3law3", "ck3law5" };
	ASSERT_EQ(expectedReturnedLaws, ck3Laws);
}