#include "ProvinceMapper.h"
#include "GameVersion.h"
#include "../../Configuration/Configuration.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "ProvinceMapping.h"
#include <filesystem>
#include <fstream>
#include <stdexcept>
namespace fs = std::filesystem;

mappers::ProvinceMapper::ProvinceMapper()
{
	LOG(LogLevel::Info) << "-> Parsing province mappings";
	registerKeys();
	parseFile("configurables/province_mappings.txt");
	clearRegisteredKeywords();
	createMappings();
	LOG(LogLevel::Info) << "<> " << theMappings.getMappings().size() << " mappings loaded.";
}

mappers::ProvinceMapper::ProvinceMapper(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
	createMappings();
}

void mappers::ProvinceMapper::registerKeys()
{
	registerRegex("[0-9\\.]+", [this](const std::string& unused, std::istream& theStream) {
		// We support only a single, current version, so eu4-vic2 style multiple versions
		// have been cut. There should only be a single, 0.0.0.0={} block inside province_mappings.txt
		theMappings = ProvinceMappingsVersion(theStream);
	});
	registerRegex("[a-zA-Z0-9\\_.:]+", commonItems::ignoreItem);
}

void mappers::ProvinceMapper::createMappings()
{
	for (const auto& mapping: theMappings.getMappings())
	{
		// fix deliberate errors where we leave mappings without keys (asian wasteland comes to mind):
		if (mapping.getImpProvinces().empty())
			continue;
		if (mapping.getCK3Provinces().empty())
			continue;

		for (const auto& impNumber: mapping.getImpProvinces())
		{
			if (impNumber)
				ImpToCK3ProvinceMap.insert(std::make_pair(impNumber, mapping.getCK3Provinces()));
		}
		for (const auto& ck3Number: mapping.getCK3Provinces())
		{
			if (ck3Number)
				CK3ToImpProvinceMap.insert(std::make_pair(ck3Number, mapping.getImpProvinces()));
		}
	}
}

std::vector<int> mappers::ProvinceMapper::getImperatorProvinceNumbers(const int ck3ProvinceNumber) const
{
	const auto& mapping = CK3ToImpProvinceMap.find(ck3ProvinceNumber);
	if (mapping != CK3ToImpProvinceMap.end())
		return mapping->second;
	return std::vector<int>();
}

std::vector<int> mappers::ProvinceMapper::getCK3ProvinceNumbers(const int impProvinceNumber) const
{
	const auto& mapping = ImpToCK3ProvinceMap.find(impProvinceNumber);
	if (mapping != ImpToCK3ProvinceMap.end())
		return mapping->second;
	return std::vector<int>();
}

void mappers::ProvinceMapper::determineValidProvinces(const Configuration& theConfiguration)
{
	LOG(LogLevel::Info) << "-> Loading Valid Provinces";
	std::ifstream definitionFile(fs::u8path(theConfiguration.getCK3Path() + "/game/map_data/definition.csv"));
	if (!definitionFile.is_open())
		throw std::runtime_error("Could not open <ck3>/game/map/definition.csv");

	char input[256];
	while (!definitionFile.eof())
	{
		definitionFile.getline(input, 255);
		std::string inputStr(input);
		if (inputStr.size() < 2)
		{
			continue;
		}
		auto provNum = std::stoi(inputStr.substr(0, inputStr.find_first_of(';')));
		validCK3Provinces.insert(provNum);
	}
	LOG(LogLevel::Info) << "<> " << validCK3Provinces.size() << " valid provinces located.";
}
