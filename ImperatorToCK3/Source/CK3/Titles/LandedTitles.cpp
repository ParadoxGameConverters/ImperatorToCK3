#include "LandedTitles.h"
#include "Title.h"
#include "Log.h"
#include "ParserHelpers.h"
#include <memory>

// This is a recursive class that scrapes 00_landed_titles.txt (and related files) looking for title colors, landlessness,
// and most importantly relation between baronies and barony provinces so we can link titles to actual clay.
// Since titles are nested according to hierarchy we do this recursively.


void CK3::LandedTitles::loadTitles(const std::string& fileName)
{
	registerKeys();
	parseFile(fileName);
	clearRegisteredKeywords();
}
void CK3::LandedTitles::loadTitles(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void CK3::LandedTitles::registerKeys()
{
	registerRegex(R"((e|k|d|c|b)_[A-Za-z0-9_\-\']+)", [this](const std::string& titleNameStr, std::istream& theStream) {
		// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
		auto newTitle = std::make_shared<Title>(titleNameStr);
		newTitle->loadTitles(theStream);

		for (auto& [locatedTitleName, locatedTitle] : newTitle->foundTitles)
		{
			if (newTitle->titleName.find("c_") == 0) // has county prefix = is a county
			{
				auto baronyProvince = locatedTitle->getProvince();
				if (baronyProvince)
				{
					if (locatedTitleName == newTitle->capitalBarony)
					{
						newTitle->capitalBaronyProvince = *baronyProvince;
					}
					newTitle->addCountyProvince(*baronyProvince); // add found baronies' provinces to countyProvinces
				}
			}
			foundTitles[locatedTitleName] = locatedTitle;
			if (!foundTitles[locatedTitleName]->getDeJureLiege()) // locatedTitle has no de jure liege set yet, which indicated it's newTitle's direct de jure vassal
				foundTitles[locatedTitleName]->setDeJureLiege(newTitle);
		}
		// now that all titles under newTitle have been moved to main foundTitles, newTitle's foundTitles can be cleared
		newTitle->foundTitles.clear();

		// And then add this one as well, overwriting existing.
		foundTitles[newTitle->titleName] = newTitle;
		});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


std::optional<std::string> CK3::LandedTitles::getCountyForProvince(const unsigned long long provinceID)
{
	for (const auto& [titleName, title] : foundTitles)
	{
		if (titleName.find_first_of("c_") == 0 && !title->getCountyProvinces().empty())
			if (title->getCountyProvinces().count(provinceID)) return titleName;
	}
	return std::nullopt;
}


void CK3::LandedTitles::insertTitle(const std::shared_ptr<Title>& title)
{
	if (!title->titleName.empty()) foundTitles[title->titleName] = title;
	else Log(LogLevel::Warning) << "Not inserting a title with empty name!";
}
void CK3::LandedTitles::eraseTitle(const std::string& name)
{
	auto titleItr = foundTitles.find(name);
	if (titleItr != foundTitles.end())
	{
		auto liegePtr = titleItr->second->getDeJureLiege();
		if (liegePtr) liegePtr->deJureVassals.erase(name);

		liegePtr = titleItr->second->getDeFactoLiege();
		if (liegePtr) liegePtr->deFactoVassals.erase(name);

		for (auto& [vassalTitleName, vassalTitle] : titleItr->second->deJureVassals)
		{
			vassalTitle->setDeJureLiege(nullptr);
		}
		for (auto& [vassalTitleName, vassalTitle] : titleItr->second->deFactoVassals)
		{
			vassalTitle->setDeFactoLiege(nullptr);
		}
	}
	foundTitles.erase(name);
}