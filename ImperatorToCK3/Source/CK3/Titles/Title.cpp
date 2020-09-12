#include "Title.h"
#include "../../Imperator/Characters/Character.h"
#include "../../Imperator/Provinces/Province.h"
#include "../../Imperator/Countries/Country.h"
#include "../../Mappers/CultureMapper/CultureMapper.h"
#include "../../Mappers/ProvinceMapper/ProvinceMapper.h"
#include "../../Mappers/ReligionMapper/ReligionMapper.h"
#include "../../Mappers/CoaMapper/CoaMapper.h"
#include "../Province/CK3Province.h"
#include "CommonFunctions.h"
#include "Log.h"


void CK3::Title::initializeFromTag(std::string theTitle, std::shared_ptr<ImperatorWorld::Country> theCountry, mappers::LocalizationMapper& localizationMapper, LandedTitles& _landedTitles, mappers::ProvinceMapper& provinceMapper,
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
			capitalCounty = _landedTitles.getCountyForProvince(provMappingsForImperatorCapital[0]);
	}
	

	
	// ------------------ Country Name Locs

	auto nameSet = false;
	
	if (!nameSet && !imperatorCountry.second->getName().empty())
	{
		englishLoc = localizationMapper.getLocBlockForKey(imperatorCountry.second->getName(), mappers::langEnum::ENGLISH);
		//spanishLoc = title.second->getDisplayName();
		//french = title.second->getDisplayName();
		//german = title.second->getDisplayName();
		nameSet = true;
	}/*
	if (!nameSet)
	{
		auto nameLocalizationMatch = localizationMapper.getLocBlockForKey(title.first);
		if (nameLocalizationMatch)
		{
			localizations.insert(std::pair(tag, *nameLocalizationMatch));
			nameSet = true;
		}
	}
	if (!nameSet && !title.second->getBaseTitle().first.empty())
	{ // see if we can match vs base title.
		auto baseTitleName = title.second->getBaseTitle().first;
		auto nameLocalizationMatch = localizationMapper.getLocBlockForKey(baseTitleName);
		if (nameLocalizationMatch)
		{
			localizations.insert(std::pair(tag, *nameLocalizationMatch));
			nameSet = true;
		}
	}

	// giving up.
	if (!nameSet)
		Log(LogLevel::Warning) << tag << " help with localization! " << title.first;

	// --------------- Adjective Locs

	auto adjSet = false;

	// Pope is special, as always.
	if (title.second->isThePope())
		adjSet = true; // We'll use vanilla PAP locs.
	else if (title.second->isTheFraticelliPope())
	{
		auto adjLocalizationMatch = localizationMapper.getLocBlockForKey("d_fraticelli_adj");
		if (adjLocalizationMatch)
		{
			localizations.insert(std::pair(tag + "_ADJ", *adjLocalizationMatch));
			adjSet = true;
		}
	}

	if (!adjSet && dynastyTitleNames.count(details.primaryCulture) && actualHolder->getDynasty().first &&
		 !actualHolder->getDynasty().second->getName().empty() && title.first != "k_rum" && title.first != "k_israel" && title.first != "e_india" &&
		 (title.first.find("e_") == 0 || title.first.find("k_") == 0))
	{
		const auto& dynastyName = actualHolder->getDynasty().second->getName();
		mappers::LocBlock newblock;
		newblock.english = dynastyName; // Ottoman Africa
		newblock.spanish = "de los " + dynastyName;
		newblock.french = "des " + dynastyName;
		newblock.german = dynastyName + "-";
		localizations.insert(std::pair(tag + "_ADJ", newblock));
		adjSet = true;
	}
	if (!adjSet && !title.second->getDisplayName().empty())
	{
		mappers::LocBlock newblock;
		newblock.english = title.second->getDisplayName() + "'s"; // singular Nordarike's Africa
		newblock.spanish = "de " + title.second->getDisplayName();
		newblock.french = "de " + title.second->getDisplayName();
		newblock.german = title.second->getDisplayName() + "s";
		localizations.insert(std::pair(tag + "_ADJ", newblock));
		adjSet = true;
	}
	if (!adjSet)
	{
		auto adjLocalizationMatch = localizationMapper.getLocBlockForKey(title.first + "_adj");
		if (adjLocalizationMatch)
		{
			localizations.insert(std::pair(tag + "_ADJ", *adjLocalizationMatch));
			adjSet = true;
		}
	}
	if (!adjSet && !title.second->getBaseTitle().first.empty())
	{
		// see if we can match vs base title.
		auto baseTitleAdj = title.second->getBaseTitle().first + "_adj";
		auto adjLocalizationMatch = localizationMapper.getLocBlockForKey(baseTitleAdj);
		if (adjLocalizationMatch)
		{
			localizations.insert(std::pair(tag + "_ADJ", *adjLocalizationMatch));
			adjSet = true;
		}
		if (!adjSet && !title.second->getBaseTitle().second->getBaseTitle().first.empty())
		{
			// maybe basetitlebasetitle?
			baseTitleAdj = title.second->getBaseTitle().second->getBaseTitle().first + "_adj";
			adjLocalizationMatch = localizationMapper.getLocBlockForKey(baseTitleAdj);
			if (adjLocalizationMatch)
			{
				localizations.insert(std::pair(tag + "_ADJ", *adjLocalizationMatch));
				adjSet = true;
			}
		}
	}
	if (!adjSet)
	{
		// Maybe c_something?
		auto alternateAdj = title.second->getName() + "_adj";
		alternateAdj = "c_" + alternateAdj.substr(2, alternateAdj.length());
		auto adjLocalizationMatch = localizationMapper.getLocBlockForKey(alternateAdj);
		if (adjLocalizationMatch)
		{
			localizations.insert(std::pair(tag, *adjLocalizationMatch));
			adjSet = true;
		}
	}
	if (!adjSet)
	{
		// Or d_something?
		auto alternateAdj = title.second->getName() + "_adj";
		alternateAdj = "d_" + alternateAdj.substr(2, alternateAdj.length());
		auto adjLocalizationMatch = localizationMapper.getLocBlockForKey(alternateAdj);
		if (adjLocalizationMatch)
		{
			localizations.insert(std::pair(tag, *adjLocalizationMatch));
			adjSet = true;
		}
	}
	if (!adjSet)
		Log(LogLevel::Warning) << tag << " help with localization for adjective! " << title.first << "_adj?";


	*/
}

