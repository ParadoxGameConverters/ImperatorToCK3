#include "ImperatorToCK3Converter.h"
#include "Imperator/ImperatorWorld.h"
#include "Configuration/Configuration.h"
//#include "CK3/CK3World.h"
//#include "CK3Outputter/CK3WorldOutputter.h"
#include "Log.h"

void convertImperatorToCK3(const mappers::VersionParser& versionParser)
{
	const Configuration theConfiguration;
	
	const ImperatorWorld::World impWorld(theConfiguration);
	//CK3World::World ck3World(impWorld, theConfiguration, versionParser);
	//CK3World::outputWorld(ck3World);

	LOG(LogLevel::Info) << "* Conversion complete *";
}