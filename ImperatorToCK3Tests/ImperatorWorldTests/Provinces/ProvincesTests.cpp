#include "gtest/gtest.h"
#include <sstream>

#include "../ImperatorToCK3/Source/Imperator/Provinces/Provinces.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/Province.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/Pops.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/Pop.h"

TEST(ImperatorWorld_ProvincesTests, provincesDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::Provinces provinces(input);

	ASSERT_TRUE(provinces.getProvinces().empty());
}

TEST(ImperatorWorld_ProvincesTests, provincesCanBeLoaded)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42={}\n";
	input << "43={}\n";
	input << "}";

	const Imperator::Provinces provinces(input);
	const auto& provinceItr = provinces.getProvinces().find(42);
	const auto& provinceItr2 = provinces.getProvinces().find(43);

	ASSERT_EQ(42, provinceItr->first);
	ASSERT_EQ(42, provinceItr->second->getID());
	ASSERT_EQ(43, provinceItr2->first);
	ASSERT_EQ(43, provinceItr2->second->getID());
}

TEST(ImperatorWorld_ProvincesTests, popCanBeLinked)
{
	std::stringstream input;
	input << "={42={pop=8}}\n";
	Imperator::Provinces provinces(input);

	std::stringstream input2;
	input2 << "8={type=\"citizen\" culture=\"roman\" religion=\"paradoxian\"}\n";
	Imperator::Pops pops;
	pops.loadPops(input2);

	provinces.linkPops(pops);

	const auto& provinceItr = provinces.getProvinces().find(42);
	const auto& pop = provinceItr->second->getPops().find(8);

	ASSERT_TRUE(pop->second);
	ASSERT_EQ("citizen", pop->second->getType());
}

TEST(ImperatorWorld_ProvincesTests, multiplePopsCanBeLinked)
{
	std::stringstream input;
	input << "={\n";
	input << "43={ pop = 10}\n";
	input << "42={pop=8}\n";
	input << "44={pop= 9}\n";
	input << "}\n";
	Imperator::Provinces provinces(input);

	std::stringstream input2;
	input2 << "={\n";
	input2 << "8={type=\"citizen\" culture=\"roman\" religion=\"paradoxian\"}\n";
	input2 << "9={type=\"tribal\" culture=\"persian\" religion=\"gsg\"}\n";
	input2 << "10={type=\"freemen\" culture=\"greek\" religion=\"zoroastrian\"}\n";
	input2 << "}\n";
	Imperator::Pops pops;
	pops.loadPops(input2);

	provinces.linkPops(pops);

	const auto& provinceItr = provinces.getProvinces().find(42);
	const auto& pop = provinceItr->second->getPops().find(8);
	const auto& provinceItr2 = provinces.getProvinces().find(43);
	const auto& pop2 = provinceItr2->second->getPops().find(10);
	const auto& provinceItr3 = provinces.getProvinces().find(44);
	const auto& pop3 = provinceItr3->second->getPops().find(9);

	ASSERT_TRUE(pop->second);
	ASSERT_EQ("citizen", pop->second->getType());
	ASSERT_TRUE(pop2->second);
	ASSERT_EQ("freemen", pop2->second->getType());
	ASSERT_TRUE(pop3->second);
	ASSERT_EQ("tribal", pop3->second->getType());
}

TEST(ImperatorWorld_ProvincesTests, BrokenLinkAttemptThrowsWarning)
{
	std::stringstream input;
	input << "={\n";
	input << "42={ pop = 8 }\n";
	input << "44={ pop = 10 }\n"; /// no pop 10
	input << "}\n";
	Imperator::Provinces provinces(input);

	std::stringstream input2;
	input2 << "={\n";
	input2 << "8={type=\"citizen\" culture=\"roman\" religion=\"paradoxian\"}\n";
	input2 << "9={type=\"tribal\" culture=\"persian\" religion=\"gsg\"}\n";
	input2 << "}\n";
	Imperator::Pops pops;
	pops.loadPops(input2);

	std::stringstream log;
	auto* stdOutBuf = std::cout.rdbuf();
	std::cout.rdbuf(log.rdbuf());

	provinces.linkPops(pops);

	std::cout.rdbuf(stdOutBuf);
	auto stringLog = log.str();
	auto newLine = stringLog.find_first_of('\n');
	stringLog = stringLog.substr(0, newLine);

	ASSERT_EQ(" [WARNING] Pop ID: 10 has no definition!", stringLog);
}
