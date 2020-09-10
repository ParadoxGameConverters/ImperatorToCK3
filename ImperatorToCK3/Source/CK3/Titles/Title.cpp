#include "Title.h"
#include "../../Imperator/Characters/Character.h"
#include "../../Imperator/Provinces/Province.h"
#include "../../Imperator/Countries/Country.h"
#include "../../Mappers/CultureMapper/CultureMapper.h"
#include "../../Mappers/ProvinceMapper/ProvinceMapper.h"
#include "../../Mappers/ReligionMapper/ReligionMapper.h"
#include "../Province/CK3Province.h"
#include "CommonFunctions.h"
#include "Log.h"


void CK3::Title::initializeFromTag(std::string theTitle, std::shared_ptr<ImperatorWorld::Country> theCountry, mappers::LocalizationMapper _localizationMapper, LandedTitles& _landedTitles)
{
	localizationMapper = std::move(_localizationMapper);
	titleName = std::move(theTitle);
	if (historyCountryFile.empty())
		historyCountryFile = "history/titles/" + titleName + ".txt";
	
	if (theCountry->getColor1())
		color1 = theCountry->getColor1().value();
	if (theCountry->getColor1())
		color2 = theCountry->getColor2().value();

	auto srcCapital = theCountry->getCapital();
	if (srcCapital) capitalCounty = _landedTitles.getCountyForProvince(srcCapital.value());

	
	// ------------------ Country Name Locs

	auto nameSet = false;
	/*
	if (!nameSet && !theCountry.getName().empty())
	{
		mappers::LocBlock newblock;
		newblock.english = title.second->getDisplayName();
		newblock.spanish = title.second->getDisplayName();
		newblock.french = title.second->getDisplayName();
		newblock.german = title.second->getDisplayName();
		localizations.insert(std::pair(tag, newblock));
		nameSet = true;
	}
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
	if (!nameSet)
	{
		// Now get creative. This happens for c_titles that have localizations as b_title
		auto alternateName = title.second->getName();
		alternateName = "b_" + alternateName.substr(2, alternateName.length());
		auto nameLocalizationMatch = localizationMapper.getLocBlockForKey(alternateName);
		if (nameLocalizationMatch)
		{
			localizations.insert(std::pair(tag, *nameLocalizationMatch));
			nameSet = true;
		}
	}
	if (!nameSet)
	{
		// using capital province name?
		auto capitalName = actualHolder->getCapitalProvince().second->getName();
		if (!capitalName.empty())
		{
			mappers::LocBlock newblock;
			newblock.english = capitalName;
			newblock.spanish = capitalName;
			newblock.french = capitalName;
			newblock.german = capitalName;
			localizations.insert(std::pair(tag, newblock));
			nameSet = true;
		}
	}

	// Override for kingdoms/empires that use Dynasty Names
	std::set<std::string> dynastyTitleNames = {"turkmeni", "khazak", "uzbehk", "turkish",
		 "karluk",
		 "khitan",
		 "tuareg", "frencharab", "andalucian", "hejazi_culture", "gulf_arabic", "mahri_culture", "al_iraqiya_arabic", "omani_culture", "yemeni_culture",
		 "bedouin_arabic", "algerian", "moroccan", "tunisian"
		 "berber", "maghreb_arabic",
		 "al_suryah_arabic", "levantine_arabic",
		 "al_misr_arabic", "egyptian_arabic",
		 "andalusian_arabic",
		 "azerbaijani", "khorasani", "mazandarani", "luri", "tajik", "persian",
		 "kurdish",
		 "afghani", "afghan",
		 "baluchi", "baloch",
		 "bihari", "kochi", "bengali",
		 "oriya",
		 "assamese",
		 "pahari", "kanauji", "vindhyan", "avadhi", "hindustani",
		 "saurashtri", "gujarati", "gujurati",
		 "kashmiri", "panjabi",
		 "malvi", "rajput",
		 "sindhi",
		 "marathi",
		 "sinhala",
		 "malayalam", "tamil",
		 "telegu", "telugu",
		 "kannada"};

	std::set<std::string> hardcodedExclusions =
		 {"k_rum", "k_israel", "e_india", "e_il-khanate", "e_persia", "e_mali", "k_mali", "k_ghana", "k_songhay", "e_hre", "e_rome", "e_byzantium"};

	if (details.government == "monarchy" && dynastyTitleNames.count(details.primaryCulture) && actualHolder->getDynasty().first &&
		 !actualHolder->getDynasty().second->getName().empty() && !hardcodedExclusions.count(title.first) &&
		 (title.first.find("e_") == 0 || title.first.find("k_") == 0))
	{
		const auto& dynastyName = actualHolder->getDynasty().second->getName();
		mappers::LocBlock newblock;
		if (dynastyName.back() == 's')
			newblock.english = dynastyName;
		else
			newblock.english = dynastyName + "s";
		newblock.spanish = dynastyName;
		newblock.french = dynastyName;
		newblock.german = dynastyName;

		// If we already set a canonical name, don't just overwrite, save it for future reference (Ottoman Crimea)
		if (nameSet)
		{
			localizations.insert(std::pair("canonical", localizations[tag]));
		}

		localizations[tag] = newblock;
		details.hasDynastyName = true;
		nameSet = true;
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

