#include "Title.h"
#include "../../Imperator/Characters/Character.h"
#include "../../Imperator/Countries/Country.h"
#include "../../Mappers/ProvinceMapper/ProvinceMapper.h"
#include "../../Mappers/CoaMapper/CoaMapper.h"
#include "Log.h"


void CK3::Title::initializeFromTag(std::string theTitle, std::shared_ptr<ImperatorWorld::Country> theCountry, mappers::LocalizationMapper& localizationMapper, LandedTitles& landedTitles, mappers::ProvinceMapper& provinceMapper,
	mappers::CoaMapper& coaMapper)
{
	
	titleName = std::move(theTitle);
	if (historyCountryFile.empty())
		historyCountryFile = "history/titles/" + titleName + ".txt";

	imperatorCountry.first = theCountry->getName();
	imperatorCountry.second = std::move(theCountry);

	auto colorOpt = imperatorCountry.second->getColor1();
	if (colorOpt)
		color1 = colorOpt.value();
	colorOpt = imperatorCountry.second->getColor2();
	if (colorOpt)
		color2 = colorOpt.value();

	coa = coaMapper.getCoaForFlagName(imperatorCountry.second->getFlag());

	auto srcCapital = imperatorCountry.second->getCapital();
	if (srcCapital)
	{
		const auto provMappingsForImperatorCapital = provinceMapper.getCK3ProvinceNumbers(srcCapital.value());
		if (!provMappingsForImperatorCapital.empty())
			capitalCounty = landedTitles.getCountyForProvince(provMappingsForImperatorCapital[0]);
	}
	

	
	// ------------------ Country Name Locs

	auto nameSet = false;
	
	if (!nameSet && !imperatorCountry.second->getName().empty())
	{
		auto impNameLoc = localizationMapper.getLocBlockForKey(imperatorCountry.second->getName());
		if (impNameLoc)
		{
			auto newBlock = impNameLoc.value();
			localizations.insert(std::pair(titleName, newBlock));
			nameSet = true;
		}
	}
	if (!nameSet)
	{
		auto nameLocalizationMatch = localizationMapper.getLocBlockForKey(imperatorCountry.first);
		if (nameLocalizationMatch)
		{
			localizations.insert(std::pair(titleName, *nameLocalizationMatch));
			nameSet = true;
		}
	}
	// giving up.
	if (!nameSet)
		Log(LogLevel::Warning) << titleName << " help with localization! " << imperatorCountry.first;
	
	// --------------- Adjective Locs

	auto adjSet = false;

	if (!adjSet && !imperatorCountry.second->getName().empty())
	{
		mappers::LocBlock newBlock;
		newBlock.english = imperatorCountry.second->getName() + "'s"; // singular Nordarike's Africa
		newBlock.french = "de " + imperatorCountry.second->getName();
		newBlock.german = imperatorCountry.second->getName() + "s";
		newBlock.russian = imperatorCountry.second->getName() + "skiy"; // this is probably bad
		newBlock.spanish = "de " + imperatorCountry.second->getName();
		localizations.insert(std::pair(titleName + "_adj", newBlock));
		adjSet = true;
	}
	if (!adjSet)
	{
		auto adjLocalizationMatch = localizationMapper.getLocBlockForKey(imperatorCountry.first + "_ADJ");
		if (adjLocalizationMatch)
		{
			localizations.insert(std::pair(titleName + "_adj", *adjLocalizationMatch));
			adjSet = true;
		}
	}
	if (!adjSet)
		Log(LogLevel::Warning) << titleName << " help with localization for adjective! " << imperatorCountry.first << "_adj?";
}

