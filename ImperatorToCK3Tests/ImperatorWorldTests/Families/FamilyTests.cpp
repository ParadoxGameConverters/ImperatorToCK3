#include "gtest/gtest.h"
#include "Imperator/Families/Family.h"
#include "Imperator/Families/FamilyFactory.h"
#include "Imperator/Characters/CharacterFactory.h"
#include "Imperator/Genes/GenesDB.h"
#include <sstream>



TEST(ImperatorWorld_FamilyTests, IDCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";
	const auto theFamily = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_EQ(42, theFamily.getID());
}

TEST(ImperatorWorld_FamilyTests, cultureCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tculture=\"paradoxian\"";
	input << "}";

	const auto theFamily = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_EQ("paradoxian", theFamily.getCulture());
}

TEST(ImperatorWorld_FamilyTests, cultureDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theFamily = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_TRUE(theFamily.getCulture().empty());
}

TEST(ImperatorWorld_FamilyTests, prestigeCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tprestige=\"420.5\"";
	input << "}";

	const auto theFamily = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_EQ(420.5, theFamily.getPrestige());
}

TEST(ImperatorWorld_FamilyTests, prestigeDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theFamily = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_EQ(0, theFamily.getPrestige());
}

TEST(ImperatorWorld_FamilyTests, prestigeRatioCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tprestige_ratio=\"0.75\"";
	input << "}";

	const auto theFamily = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_EQ(0.75, theFamily.getPrestigeRatio());
}

TEST(ImperatorWorld_FamilyTests, prestigeRatioDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theFamily = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_EQ(0, theFamily.getPrestigeRatio());
}

TEST(ImperatorWorld_FamilyTests, keyCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tkey=\"paradoxian\"";
	input << "}";

	const auto theFamily = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_EQ("paradoxian", theFamily.getKey());
}


TEST(ImperatorWorld_FamilyTests, minorFamilyDefaultsToFalse)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theFamily = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_FALSE(theFamily.isMinor());
}


TEST(ImperatorWorld_FamilyTests, minorFamilyCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tminor_family=\"yes\"";
	input << "}";

	const auto theFamily = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_TRUE(theFamily.isMinor());
}


TEST(ImperatorWorld_FamilyTests, keyDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theFamily = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_TRUE(theFamily.getKey().empty());
}

TEST(ImperatorWorld_FamilyTests, membersDefaultToEmpty) {
	std::stringstream input;
	input << "= {}";

	const auto family = *Imperator::Family::Factory().getFamily(input, 42);

	ASSERT_TRUE(family.getMembers().empty());
}

TEST(ImperatorWorld_FamilyTests, linkingNullptrMemberIsLogged) {
	std::stringstream input;
	input << "= {}";

	auto family = *Imperator::Family::Factory().getFamily(input, 42);

	std::stringstream log;
	auto* stdOutBuf = std::cout.rdbuf();
	std::cout.rdbuf(log.rdbuf());

	family.linkMember(nullptr);

	std::cout.rdbuf(stdOutBuf);
	auto stringLog = log.str();
	auto newLine = stringLog.find_first_of('\n');
	stringLog = stringLog.substr(0, newLine);

	ASSERT_EQ(" [WARNING] Family 42: cannot link nullptr member!", stringLog);
}

TEST(ImperatorWorld_FamilyTests, cannotLinkMemberWithoutPreexistingMatchingID) {
	std::stringstream input;
	input << "= { member={40 50 5} }";
	auto family = *Imperator::Family::Factory().getFamily(input, 42);

	std::stringstream charInput;
	std::shared_ptr<Imperator::Character> character = Imperator::Character::Factory().getCharacter(charInput, "6", nullptr);

	std::stringstream log;
	auto* stdOutBuf = std::cout.rdbuf();
	std::cout.rdbuf(log.rdbuf());

	family.linkMember(character);

	std::cout.rdbuf(stdOutBuf);
	auto stringLog = log.str();
	auto newLine = stringLog.find_first_of('\n');
	stringLog = stringLog.substr(0, newLine);

	ASSERT_EQ(" [WARNING] Family 42: cannot link 6: not found in members!", stringLog);
}

TEST(ImperatorWorld_FamilyTests, memberLinkingWorks) {
	std::stringstream input;
	input << "= { member={40 50 5} }";
	auto family = *Imperator::Family::Factory().getFamily(input, 42);

	std::stringstream charInput;
	charInput << "= { culture = kushite }";
	auto genesDB = std::make_shared<Imperator::GenesDB>();
	std::shared_ptr<Imperator::Character> character = Imperator::Character::Factory().getCharacter(charInput, "50", genesDB);

	family.linkMember(character);
	
	ASSERT_EQ("kushite", family.getMembers()[1].second->getCulture());
}