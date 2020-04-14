#include "ImperatorToCK3Converter.h"
#include "Imperator/ImperatorWorld.h"
#include "Configuration/Configuration.h"
//#include "Ck3/Ck3World.h"
//#include "Ck3Outputter/Ck3WorldOutputter.h"
#include "Log.h"

void convertImperatorToCk3(const mappers::VersionParser& versionParser)
{
	const Configuration theConfiguration;
	
	const ImperatorWorld::World impWorld(theConfiguration);
	//Ck3World::World ck3World(impWorld, theConfiguration, versionParser);
	//Ck3World::outputWorld(ck3World);

	LOG(LogLevel::Info) << "* Conversion complete *";
}