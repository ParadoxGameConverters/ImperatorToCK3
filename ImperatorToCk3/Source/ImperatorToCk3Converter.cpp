#include "ImperatorToCk3Converter.h"
#include "Imperator/World.h"
#include "Configuration/Configuration.h"
#include "Ck3/Ck3World.h"
#include "Log.h"

void convertImperatorToCk3()
{
	const Configuration theConfiguration;
	const Imperator::World sourceWorld(theConfiguration);
	Ck3::World destWorld(sourceWorld, theConfiguration);

	LOG(LogLevel::Info) << "* Conversion complete *";
}