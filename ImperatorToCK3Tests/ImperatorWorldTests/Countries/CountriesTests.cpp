#include "gtest/gtest.h"
#include <sstream>

#include "../ImperatorToCK3/Source/Imperator/Countries/Countries.h"
#include "../ImperatorToCK3/Source/Imperator/Countries/Country.h"
#include "../ImperatorToCK3/Source/Imperator/Families/Families.h"
#include "../ImperatorToCK3/Source/Imperator/Families/Family.h"

TEST(ImperatorWorld_CountriesTests, countriesDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::Countries countries(input);

	ASSERT_TRUE(countries.getCountries().empty());
}

TEST(ImperatorWorld_CountriesTests, countriesCanBeLoaded)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42={}\n";
	input << "43={}\n";
	input << "}";

	const Imperator::Countries countries(input);
	
	const auto& countryItr = countries.getCountries().find(42);
	const auto& countryItr2 = countries.getCountries().find(43);

	ASSERT_EQ(42, countryItr->first);
	ASSERT_EQ(42, countryItr->second->getID());
	ASSERT_EQ(43, countryItr2->first);
	ASSERT_EQ(43, countryItr2->second->getID());
	ASSERT_EQ(2, countries.getCountries().size());
}


TEST(ImperatorWorld_CountriesTests, familyCanBeLinked)
{
	std::stringstream input;
	input << "={42={family=8}}\n";
	Imperator::Countries countries(input);

	std::stringstream input2;
	input2 << "8={key=\"Cornelli\" prestige=2 member={ 4479 4480}}\n";
	Imperator::Families families;
	families.loadFamilies(input2);

	countries.linkFamilies(families);

	const auto& countryItr = countries.getCountries().find(42);
	const auto& family = countryItr->second->getFamilies().find(8);

	ASSERT_TRUE(family->second);
	ASSERT_EQ(2, family->second->getPrestige());
}

TEST(ImperatorWorld_CountriesTests, multipleFamiliesCanBeLinked)
{
	std::stringstream input;
	input << "={\n";
	input << "43={ family = 10}\n";
	input << "42={family=8}\n";
	input << "44={minor_family= 9}\n";
	input << "}\n";
	Imperator::Countries countries(input);

	std::stringstream input2;
	input2 << "={\n";
	input2 << "8={key=\"Cornelli\" prestige=2 member={ 4479 4480} }\n";
	input2 << "9={key=\"minor_bmb\" prestige=69 minor_family=yes member={ 4479 4480} }\n";
	input2 << "10={key=\"minor_rom\" prestige=7 minor_family=yes member={ 69 420} }\n";
	input2 << "}\n";
	Imperator::Families families;
	families.loadFamilies(input2);

	countries.linkFamilies(families);

	const auto& countryItr = countries.getCountries().find(42);
	const auto& family = countryItr->second->getFamilies().find(8);
	const auto& countryItr2 = countries.getCountries().find(43);
	const auto& family2 = countryItr2->second->getFamilies().find(10);
	const auto& countryItr3 = countries.getCountries().find(44);
	const auto& family3 = countryItr3->second->getFamilies().find(9);

	ASSERT_TRUE(family->second);
	ASSERT_EQ(2, family->second->getPrestige());
	ASSERT_TRUE(family2->second);
	ASSERT_EQ(7, family2->second->getPrestige());
	ASSERT_TRUE(family3->second);
	ASSERT_EQ(69, family3->second->getPrestige());
}

TEST(ImperatorWorld_CountriesTests, BrokenLinkAttemptThrowsWarning)
{
	std::stringstream input;
	input << "={\n";
	input << "42={ family = 8 }\n";
	input << "44={ minor_family = 10 }\n"; /// no pop 10
	input << "}\n";
	Imperator::Countries countries(input);

	std::stringstream input2;
	input2 << "={\n";
	input2 << "8={key=\"Cornelli\" prestige=0 member={ 4479 4480}}\n";
	input2 << "9={key=\"minor_bmb\" prestige=0 minor_family=yes member={ 4479 4480}}\n";
	input2 << "}\n";
	Imperator::Families families;
	families.loadFamilies(input2);

	std::stringstream log;
	auto* stdOutBuf = std::cout.rdbuf();
	std::cout.rdbuf(log.rdbuf());

	countries.linkFamilies(families);

	std::cout.rdbuf(stdOutBuf);
	auto stringLog = log.str();
	auto newLine = stringLog.find_first_of('\n');
	stringLog = stringLog.substr(0, newLine);

	ASSERT_EQ(" [WARNING] Family ID: 10 has no definition!", stringLog);
}