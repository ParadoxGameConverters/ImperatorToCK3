#include "LandedTitles.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "Title.h"

// This is a recursive class that scrapes 00_landed_titles.txt (and related files) looking for title colors, landlessness,
// and most importantly relation between baronies and barony provinces so we can link titles to actual clay.
// Since titles are nested according to hierarchy we do this recursively.

void CK3::LandedTitles::loadTitles(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void CK3::LandedTitles::loadTitles(const std::string& fileName)
{
	registerKeys();
	parseFile(fileName);
	clearRegisteredKeywords();
}

void CK3::LandedTitles::registerKeys()
{
	registerRegex(R"((e|k|d|c|b)_[A-Za-z0-9_\-\']+)", [this](const std::string& titleName, std::istream& theStream) {
		// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
		auto newTitle = LandedTitles();
		newTitle.loadTitles(theStream);
		for (const auto& locatedTitle: newTitle.getFoundTitles())
		{
			if (titleName.find_first_of("c_") == 0) // has county prefix = is a county
			{
				auto baronyProvince = locatedTitle.second.getProvince();
				if (baronyProvince)
				{
					if (countyProvinces.size() == 0) capitalBarony = *baronyProvince;
					newTitle.countyProvinces.insert(*baronyProvince); // add found baronies' provinces to a countyProvinces set
				}
			}
			foundTitles[locatedTitle.first] = locatedTitle.second;
		}
		
		// And then add this one as well, overwriting existing.
		foundTitles[titleName] = newTitle;
	});
	registerKeyword("definite_form", [this](const std::string& unused, std::istream& theStream) {
		definiteForm = commonItems::singleString(theStream).getString() == "yes";
	});
	registerKeyword("landless", [this](const std::string& unused, std::istream& theStream) {
		landless = commonItems::singleString(theStream).getString() == "yes";
	});
	registerKeyword("color", [this](const std::string& unused, std::istream& theStream) {
		color = laFabricaDeColor.getColor(theStream);
	});
	registerKeyword("capital", [this](const std::string& unused, std::istream& theStream) {
		capital = std::make_pair(commonItems::singleString(theStream).getString(), nullptr);
	});
	registerKeyword("province", [this](const std::string& unused, std::istream& theStream) {
		province = commonItems::singleInt(theStream).getInt();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

std::optional<std::string> CK3::LandedTitles::getCountyForProvince(const int provinceID)
{
	for (const auto& [titleName, title] : foundTitles)
	{
		if (titleName.find_first_of("c_") == 0 && !title.countyProvinces.empty())
			if (title.countyProvinces.count(provinceID)) return titleName;
	}
	return std::nullopt;
}
