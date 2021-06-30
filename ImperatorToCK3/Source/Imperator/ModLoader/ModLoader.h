#ifndef IMPERATOR_MODSLOADER_H
#define IMPERATOR_MODSLOADER_H



#include <map>
#include <vector>
#include <optional>
#include <string>



class Configuration;


namespace Imperator {

using Mods = std::map<std::string, std::string>;
using ModPaths = std::vector<std::string>;

class ModLoader {
  public:
	ModLoader() = default;

	void loadMods(const Configuration& configuration, const ModPaths& incomingMods);

	[[nodiscard]] const auto& getMods() const { return usableMods; }

  private:
	void loadImperatorModDirectory(const Configuration& configuration, const ModPaths& incomingMods);

	[[nodiscard]] std::optional<std::string> getModPath(const std::string& modName) const;
	[[nodiscard]] bool extractZip(const std::string& archive, const std::string& path) const;

	Mods possibleMods;			  // name, absolute path to mod directory
	Mods possibleCompressedMods;  // name, absolute path to zip file
	Mods usableMods;			  // name, absolute path for directories, relative for unpacked
};

}  // namespace Imperator



#endif	// IMPERATOR_MODSLOADER_H