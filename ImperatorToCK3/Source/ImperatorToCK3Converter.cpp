#include "ImperatorToCK3Converter.h"
#include "Imperator/ImperatorWorld.h"
#include "Configuration/Configuration.h"
#include "CK3/CK3World.h"
#include "CK3Outputter/CK3WorldOutputter.h"
#include "Log.h"
#include "ConverterVersion.h"



void convertImperatorToCK3(const commonItems::ConverterVersion& converterVersion) {
	const Configuration theConfiguration;
	
	const Imperator::World impWorld(theConfiguration);
	const CK3::World ck3World(impWorld, theConfiguration, converterVersion);
	CK3::outputWorld(ck3World, theConfiguration);

	LOG(LogLevel::Info) << "* Conversion complete *";
}