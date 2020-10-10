#include "Title.h"
#include "../../Imperator/Characters/Character.h"
#include "../../Imperator/Countries/Country.h"
#include "../../Mappers/ProvinceMapper/ProvinceMapper.h"
#include "../../Mappers/CoaMapper/CoaMapper.h"
#include "../../Mappers/TagTitleMapper/TagTitleMapper.h"
#include "Log.h"

void CK3::Title::initializeFromTag(std::shared_ptr<ImperatorWorld::Country> theCountry, mappers::LocalizationMapper& localizationMapper, LandedTitles& landedTitles, mappers::ProvinceMapper& provinceMapper,
	mappers::CoaMapper& coaMapper, mappers::TagTitleMapper& tagTitleMapper)
{
	generated = true;

	imperatorCountry = std::move(theCountry);

	
	// ------------------ determine CK3 title
	
	std::optional<mappers::LocBlock> validatedName;
	// hard code for Antigonid Kingdom, Seleucid Empire and Maurya (which use customizable localization for name and adjective)
	if (imperatorCountry->getTag() == "PRY") validatedName = localizationMapper.getLocBlockForKey("get_pry_name_fallback");
	else if (imperatorCountry->getTag() == "SEL") validatedName = localizationMapper.getLocBlockForKey("get_sel_name_fallback");
	else if (imperatorCountry->getTag() == "MRY") validatedName = localizationMapper.getLocBlockForKey("get_mry_name_fallback");
	// normal case
	else validatedName = localizationMapper.getLocBlockForKey(imperatorCountry->getName());

	std::optional<std::string> title;
	if (validatedName) 
		title = tagTitleMapper.getTitleForTag(imperatorCountry->getTag(), imperatorCountry->getCountryRank(), validatedName->english);
	else
		title = tagTitleMapper.getTitleForTag(imperatorCountry->getTag(), imperatorCountry->getCountryRank());
	
	if (!title)
		throw std::runtime_error("Country " + imperatorCountry->getTag() + " could not be mapped!");
	titleName = *title;


	// ------------------ determine other attributes
	
	if (historyCountryFile.empty())
		historyCountryFile = "history/titles/" + titleName + ".txt";


	if (imperatorCountry->getMonarch()) holder = "imperator" + std::to_string(*imperatorCountry->getMonarch());

	auto colorOpt = imperatorCountry->getColor1();
	if (colorOpt)
		color1 = *colorOpt;
	colorOpt = imperatorCountry->getColor2();
	if (colorOpt)
		color2 = *colorOpt;

	coa = coaMapper.getCoaForFlagName(imperatorCountry->getFlag());

	auto srcCapital = imperatorCountry->getCapital();
	if (srcCapital)
	{
		const auto provMappingsForImperatorCapital = provinceMapper.getCK3ProvinceNumbers(*srcCapital);
		if (!provMappingsForImperatorCapital.empty())
			capitalCounty = landedTitles.getCountyForProvince(provMappingsForImperatorCapital[0]);
	}
	

	
	// ------------------ Country Name Locs

	auto nameSet = false;
	if (validatedName)
	{
		localizations.insert(std::pair(titleName, *validatedName));
		nameSet = true;
	}
	if (!nameSet)
	{
		auto impTagLoc = localizationMapper.getLocBlockForKey(imperatorCountry->getTag());
		if (impTagLoc)
		{
			localizations.insert(std::pair(titleName, *impTagLoc));
			nameSet = true;
		}
	}
	// giving up.
	if (!nameSet)
		Log(LogLevel::Warning) << titleName << " help with localization! " << imperatorCountry->getName() << "?";
	
	// --------------- Adjective Locs
	trySetAdjectiveLoc(localizationMapper);

}

void CK3::Title::trySetAdjectiveLoc(mappers::LocalizationMapper& localizationMapper)
{
	auto adjSet = false;

	if (imperatorCountry->getTag() == "PRY" || imperatorCountry->getTag() == "SEL" || imperatorCountry->getTag() == "MRY") // these tags use customizable loc for adj
	{
		std::optional<mappers::LocBlock> validatedAdj;
		if (imperatorCountry->getTag() == "PRY") validatedAdj = localizationMapper.getLocBlockForKey("get_pry_adj_fallback");
		else if (imperatorCountry->getTag() == "SEL") validatedAdj = localizationMapper.getLocBlockForKey("get_sel_adj_fallback");
		else if (imperatorCountry->getTag() == "MRY") validatedAdj = localizationMapper.getLocBlockForKey("get_mry_adj_fallback");

		if (validatedAdj)
		{
			localizations.insert(std::pair(titleName + "_adj", *validatedAdj));
			adjSet = true;
		}
	}
	if (!adjSet)
	{
		auto adjLocalizationMatch = localizationMapper.getLocBlockForKey(imperatorCountry->getName() + "_ADJ");
		if (adjLocalizationMatch)
		{
			localizations.insert(std::pair(titleName + "_adj", *adjLocalizationMatch));
			adjSet = true;
		}
	}
	if (!adjSet && !imperatorCountry->getName().empty()) // if loc for <title name>_adj key doesn't exist, use title name (which is apparently what Imperator does
	{
		auto adjLocalizationMatch = localizationMapper.getLocBlockForKey(imperatorCountry->getName());
		if (adjLocalizationMatch)
		{
			localizations.insert(std::pair(titleName + "_adj", *adjLocalizationMatch));
			adjSet = true;
		}
	}
	if (!adjSet) // same as above, but with tag instead of name as fallback
	{
		auto adjLocalizationMatch = localizationMapper.getLocBlockForKey(imperatorCountry->getTag());
		if (adjLocalizationMatch)
		{
			localizations.insert(std::pair(titleName + "_adj", *adjLocalizationMatch));
			adjSet = true;
		}
	}
	// giving up.
	if (!adjSet)
		Log(LogLevel::Warning) << titleName << " help with localization for adjective! " << imperatorCountry->getName() << "_adj?";
}