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

		Title::addFoundTitle(newTitle, foundTitles);
		});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


std::optional<std::string> CK3::LandedTitles::getCountyForProvince(const unsigned long long provinceID)
{
	for (const auto& [titleName, title] : foundTitles)
	{
		if (titleName.starts_with("c_") && !title->getCountyProvinces().empty())
			if (title->getCountyProvinces().contains(provinceID)) return titleName;
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
	if (auto titleItr = foundTitles.find(name); titleItr != foundTitles.end())
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