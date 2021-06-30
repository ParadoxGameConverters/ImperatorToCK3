#include "Imperator/ModLoader/ModLoader.h"
#include "Configuration/Configuration.h"
#include "ConverterVersion.h"
#include "OSCompatibilityLayer.h"
#include "gtest/gtest.h"
#include <gmock/gmock-matchers.h>



using testing::Pair;
using testing::UnorderedElementsAre;


TEST(ImperatorWorld_ModLoaderTests, ModsCanBeLocatedUnpackedAndUpdated) {
	std::stringstream configurationInput;
	configurationInput << "ImperatorDocDirectory = \"TestFiles\"\n";
	//configurationInput << "ImperatorDirectory = \"TestFiles/eu4installation\"\n";
	//configurationInput << "CK3directory = \"TestFiles/vic3installation\"\n";
	const auto configuration = Configuration(configurationInput);

	Imperator::ModPaths mods;						 // this is what comes from the save
	mods.emplace_back("mod/themod.mod");			// mod's in fact named "The Mod" in the file.

	Imperator::ModLoader modLoader;
	modLoader.loadMods(configuration, mods);
	auto modMap = modLoader.getMods();

	EXPECT_THAT(modMap, UnorderedElementsAre(Pair("The Mod", "TestFiles/mod/themod/")));
}

TEST(ImperatorWorld_ModLoaderTests, BrokenMissingAndNonexistentModsAreDiscarded) {
	std::stringstream configurationInput;
	configurationInput << "ImperatorDocDirectory = \"TestFiles\"\n";
	//configurationInput << "ImperatorDirectory = \"TestFiles/eu4installation\"\n";
	//configurationInput << "CK3directory = \"TestFiles/vic3installation\"\n";
	const auto configuration = Configuration(configurationInput);

	Imperator::ModPaths mods;
	mods.emplace_back("mod/themod.mod");
	mods.emplace_back("mod/brokenmod.mod");	 // no path
	mods.emplace_back("mod/missingmod.mod");  // missing directory
	mods.emplace_back("mod/nonexistentmod.mod");  // doesn't exist.

	Imperator::ModLoader modLoader;
	modLoader.loadMods(configuration, mods);
	auto modMap = modLoader.getMods();

	EXPECT_THAT(modMap, UnorderedElementsAre(Pair("The Mod", "TestFiles/mod/themod/")));
}

TEST(ImperatorWorld_ModLoaderTests, CompressedModsCanBeUnpacked) {
	std::stringstream configurationInput;
	configurationInput << "ImperatorDocDirectory = \"TestFiles\"\n";
	//configurationInput << "ImperatorDirectory = \"TestFiles/eu4installation\"\n";
	//configurationInput << "CK3directory = \"TestFiles/vic3installation\"\n";
	const auto configuration = Configuration(configurationInput);

	Imperator::ModPaths mods;
	mods.emplace_back("mod/packedmod.mod");

	Imperator::ModLoader modLoader;
	modLoader.loadMods(configuration, mods);
	auto modMap = modLoader.getMods();

	EXPECT_THAT(modMap, UnorderedElementsAre(Pair("Packed Mod", "mods/packedmod/")));
	EXPECT_TRUE(commonItems::DoesFolderExist("mods/packedmod/"));
}
