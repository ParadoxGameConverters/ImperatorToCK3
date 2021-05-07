#include "Title.h"
#include "LandedTitles.h"
#include "TitlesHistory.h"
#include "CK3/Character/CK3Character.h"
#include "Imperator/Countries/Country.h"
#include "Imperator/Countries/CountryName.h"
#include "Mappers/CoaMapper/CoaMapper.h"
#include "Mappers/GovernmentMapper/GovernmentMapper.h"
#include "Mappers/ProvinceMapper/ProvinceMapper.h"
#include "Mappers/SuccessionLawMapper/SuccessionLawMapper.h"
#include "Mappers/TagTitleMapper/TagTitleMapper.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include <ranges>
#include <utility>



void CK3::Title::addFoundTitle(const std::shared_ptr<Title>& newTitle, std::map<std::string, std::shared_ptr<Title>>& foundTitles) {
	for (const auto& [locatedTitleName, locatedTitle] : newTitle->foundTitles) {
		if (newTitle->getRank() == TitleRank::county) {
			const auto& baronyProvince = locatedTitle->getProvince();
			if (baronyProvince) {
				if (locatedTitleName == newTitle->capitalBarony) {
					newTitle->capitalBaronyProvince = *baronyProvince;
				}
				newTitle->addCountyProvince(*baronyProvince); // add found baronies' provinces to countyProvinces
			}
		}
		foundTitles[locatedTitleName] = locatedTitle;
	}
	// now that all titles under newTitle have been moved to main foundTitles, newTitle's foundTitles can be cleared
	newTitle->foundTitles.clear();

	// And then add this one as well, overwriting existing.
	foundTitles[newTitle->titleName] = newTitle;
}


void CK3::Title::loadTitles(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void CK3::Title::setHolder(const std::shared_ptr<CK3::Character>& newPtr) {
	if (newPtr) {
		history.holder = newPtr->ID;
	}
	else {
		history.holder = "0";
	}
	holderPtr = newPtr;
}


void CK3::Title::registerKeys() {
	registerRegex(R"((k|d|c|b)_[A-Za-z0-9_\-\']+)", [this](const std::string& titleNameStr, std::istream& theStream) {
		// Pull the titles beneath this one and add them to the lot, overwriting existing ones.
		auto newTitle = std::make_shared<Title>(titleNameStr);
		newTitle->loadTitles(theStream);
		
		if (newTitle->getRank() == TitleRank::barony && capitalBarony.empty()) { // title is a barony, and no other barony has been found in this scope yet
			capitalBarony = newTitle->titleName;
		}
		
		addFoundTitle(newTitle, foundTitles);
		newTitle->setDeJureLiege(shared_from_this());
	});
	registerKeyword("definite_form", [this](std::istream& theStream) {
		definiteForm = commonItems::getString(theStream) == "yes";
	});
	registerKeyword("landless", [this](std::istream& theStream) {
		landless = commonItems::getString(theStream) == "yes";
	});
	registerKeyword("color", [this](std::istream& theStream) {
		color = laFabricaDeColor.getColor(theStream);
	});
	registerKeyword("capital", [this](std::istream& theStream) {
		capitalCounty = std::make_pair(commonItems::getString(theStream), nullptr);
	});
	registerKeyword("province", [this](std::istream& theStream) {
		province = commonItems::getULlong(theStream);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


CK3::Title::Title(std::string name): titleName(std::move(name)) {
	setRank();
}


void CK3::Title::initializeFromTag(std::shared_ptr<Imperator::Country> theCountry,
								   const std::map<unsigned long long, std::shared_ptr<Imperator::Country>>& imperatorCountries,
								   mappers::LocalizationMapper& localizationMapper,
								   LandedTitles& landedTitles,
								   mappers::ProvinceMapper& provinceMapper,
								   mappers::CoaMapper& coaMapper,
								   mappers::TagTitleMapper& tagTitleMapper,
								   mappers::GovernmentMapper& governmentMapper, 
								   mappers::SuccessionLawMapper& successionLawMapper)
{
	importedOrUpdatedFromImperator = true;

	imperatorCountry = std::move(theCountry);

	
	// ------------------ determine CK3 title
	
	std::optional<mappers::LocBlock> validatedName;
	// hard code for Antigonid Kingdom, Seleucid Empire and Maurya (which use customizable localization for name and adjective)
	if (imperatorCountry->getName() == "PRY_DYN")
		validatedName = localizationMapper.getLocBlockForKey("get_pry_name_fallback");
	else if (imperatorCountry->getName() == "SEL_DYN")
		validatedName = localizationMapper.getLocBlockForKey("get_sel_name_fallback");
	else if (imperatorCountry->getName() == "MRY_DYN")
		validatedName = localizationMapper.getLocBlockForKey("get_mry_name_fallback");
	// normal case
	else
		validatedName = imperatorCountry->getCountryName().getNameLocBlock(localizationMapper, imperatorCountries);

	std::optional<std::string> title;
	if (validatedName) 
		title = tagTitleMapper.getTitleForTag(imperatorCountry->getTag(), imperatorCountry->getCountryRank(), validatedName->english);
	else
		title = tagTitleMapper.getTitleForTag(imperatorCountry->getTag(), imperatorCountry->getCountryRank());
	if (!title)
		throw std::runtime_error("Country " + imperatorCountry->getTag() + " could not be mapped!");
	titleName = *title;

	setRank();


	
	// ------------------ determine holder
	if (imperatorCountry->getMonarch())
		history.holder = "imperator" + std::to_string(*imperatorCountry->getMonarch());

	// ------------------ determine government
	if (imperatorCountry->getGovernment())
		history.government = governmentMapper.getCK3GovernmentForImperatorGovernment(*imperatorCountry->getGovernment());

	// ------------------ determine color
	if (const auto& colorOpt1 = imperatorCountry->getColor1(); colorOpt1) {
		color1 = *colorOpt1;
	}
	if (const auto& colorOpt2 = imperatorCountry->getColor2(); colorOpt2) {
		color2 = *colorOpt2;
	}

	// determine successions laws
	successionLaws = successionLawMapper.getCK3LawsForImperatorLaws(imperatorCountry->getLaws());

	// ------------------ determine CoA
	coa = coaMapper.getCoaForFlagName(imperatorCountry->getFlag());
	
	// ------------------ determine other attributes
	
	const auto& srcCapital = imperatorCountry->getCapital();
	if (srcCapital) {
		const auto provMappingsForImperatorCapital = provinceMapper.getCK3ProvinceNumbers(*srcCapital);
		if (!provMappingsForImperatorCapital.empty()) {
			auto foundCounty = landedTitles.getCountyForProvince(provMappingsForImperatorCapital.at(0));
			if (foundCounty) {
				capitalCounty = std::make_pair(*foundCounty, nullptr);
			}
		}
	}
	

	
	// ------------------ Country Name Locs

	auto nameSet = false;
	if (validatedName) {
		localizations.emplace(titleName, *validatedName);
		nameSet = true;
	}
	if (!nameSet) {
		auto impTagLoc = localizationMapper.getLocBlockForKey(imperatorCountry->getTag());
		if (impTagLoc) {
			localizations.emplace(titleName, *impTagLoc);
			nameSet = true;
		}
	}
	// giving up
	if (!nameSet)
		Log(LogLevel::Warning) << titleName << " needs help with localization! " << imperatorCountry->getName() << "?";
	
	// --------------- Adjective Locs
	trySetAdjectiveLoc(localizationMapper, imperatorCountries);
}


void CK3::Title::updateFromTitle(const std::shared_ptr<Title>& otherTitle) {
	if (titleName != otherTitle->titleName) {
		Log(LogLevel::Error) << titleName << " can not be updated from  " << otherTitle->titleName << ": different title names!";
		return;
	}
	titleName = otherTitle->titleName;
	localizations = otherTitle->localizations;
	
	importedOrUpdatedFromImperator = otherTitle->importedOrUpdatedFromImperator;
	imperatorCountry = otherTitle->imperatorCountry;

	history = otherTitle->history;

	color1 = otherTitle->color1;
	color2 = otherTitle->color2;
	coa = otherTitle->coa;

	capitalCounty = otherTitle->capitalCounty;
}


void CK3::Title::trySetAdjectiveLoc(mappers::LocalizationMapper& localizationMapper, const std::map<unsigned long long, std::shared_ptr<Imperator::Country>>& imperatorCountries) {
	auto adjSet = false;

	if (imperatorCountry->getTag() == "PRY" || imperatorCountry->getTag() == "SEL" || imperatorCountry->getTag() == "MRY") { // these tags use customizable loc for adj
		std::optional<mappers::LocBlock> validatedAdj;
		if (imperatorCountry->getName() == "PRY_DYN")
			validatedAdj = localizationMapper.getLocBlockForKey("get_pry_adj_fallback");
		else if (imperatorCountry->getName() == "SEL_DYN")
			validatedAdj = localizationMapper.getLocBlockForKey("get_sel_adj_fallback");
		else if (imperatorCountry->getName() == "MRY_DYN")
			validatedAdj = localizationMapper.getLocBlockForKey("get_mry_adj_fallback");

		if (validatedAdj) {
			localizations.emplace(titleName + "_adj", *validatedAdj);
			adjSet = true;
		}
	}
	if (!adjSet) {
		const auto adjOpt = imperatorCountry->getCountryName().getAdjectiveLocBlock(localizationMapper, imperatorCountries);
		if (adjOpt) {
			localizations.emplace(titleName + "_adj", *adjOpt);
			adjSet = true;
		}
	}
	if (!adjSet) { // final fallback
		auto adjLocalizationMatch = localizationMapper.getLocBlockForKey(imperatorCountry->getTag());
		if (adjLocalizationMatch) {
			localizations.emplace(titleName + "_adj", *adjLocalizationMatch);
			adjSet = true;
		}
	}
	// giving up
	if (!adjSet)
		Log(LogLevel::Warning) << titleName << " needs help with localization for adjective! " << imperatorCountry->getName() << "_adj?";
}

void CK3::Title::setRank() {
	if (titleName.starts_with('b'))
		rank = TitleRank::barony;
	else if (titleName.starts_with('c'))
		rank = TitleRank::county;
	else if (titleName.starts_with('d'))
		rank = TitleRank::duchy;
	else if (titleName.starts_with('k'))
		rank = TitleRank::kingdom;
	else if (titleName.starts_with('e'))
		rank = TitleRank::empire;
	else
		throw std::runtime_error("Title " + titleName + ": unknown rank!");
}


void CK3::Title::setDeJureLiege(const std::shared_ptr<Title>& liegeTitle) {
	deJureLiege = liegeTitle;
	if (deJureLiege)
		liegeTitle->deJureVassals[titleName] = shared_from_this(); // reference: https://www.nextptr.com/tutorial/ta1414193955/enable_shared_from_this-overview-examples-and-internals
}


void CK3::Title::setDeFactoLiege(const std::shared_ptr<Title>& liegeTitle) {
	deFactoLiege = liegeTitle;
	if (deFactoLiege) {
		history.liege = deFactoLiege->getName();
		liegeTitle->deFactoVassals[titleName] = shared_from_this(); // reference: https://www.nextptr.com/tutorial/ta1414193955/enable_shared_from_this-overview-examples-and-internals
	}
	else {
		history.liege = std::nullopt;
	}
}


std::optional<int> CK3::Title::getOwnOrInheritedDevelopmentLevel() const {
	if (history.developmentLevel) // if development level is already set, just return it
		return history.developmentLevel;
	if (deJureLiege) //if de jure liege exists, return their level
		return deJureLiege->getOwnOrInheritedDevelopmentLevel();
	return std::nullopt;
}


std::map<std::string, std::shared_ptr<CK3::Title>> CK3::Title::getDeJureVassalsAndBelow(const std::string& rankFilter) const {
	std::map<std::string, std::shared_ptr<Title>> deJureVassalsAndBelow;
	for (const auto& [vassalTitleName, vassalTitle] : deJureVassals) {
		// add the direct part
		if (vassalTitleName.find_first_of(rankFilter) == 0)
			deJureVassalsAndBelow[vassalTitleName] = vassalTitle;

		// add the "below" part (recursive)
		auto belowTitles = vassalTitle->getDeJureVassalsAndBelow(rankFilter);
		for (auto& [belowTitleName, belowTitle] : belowTitles) {
			if (belowTitleName.find_first_of(rankFilter) == 0)
				deJureVassalsAndBelow[belowTitleName] = belowTitle;
		}
	}
	return deJureVassalsAndBelow;
}


std::map<std::string, std::shared_ptr<CK3::Title>> CK3::Title::getDeFactoVassalsAndBelow(const std::string& rankFilter) const {
	std::map<std::string, std::shared_ptr<Title>> deFactoVassalsAndBelow;
	for (const auto& [vassalTitleName, vassalTitle] : deFactoVassals) {
		// add the direct part
		if (vassalTitleName.find_first_of(rankFilter) == 0) deFactoVassalsAndBelow[vassalTitleName] = vassalTitle;

		// add the "below" part (recursive)
		auto belowTitles = vassalTitle->getDeFactoVassalsAndBelow(rankFilter);
		for (auto& [belowTitleName, belowTitle] : belowTitles) {
			if (belowTitleName.find_first_of(rankFilter) == 0) deFactoVassalsAndBelow[belowTitleName] = belowTitle;
		}
	}
	return deFactoVassalsAndBelow;
}


void CK3::Title::addHistory(const LandedTitles& landedTitles, TitleHistory titleHistory) {
	history = std::move(titleHistory);

	if (history.liege) {
		const auto liegeItr = landedTitles.getTitles().find(*history.liege);
		if (liegeItr != landedTitles.getTitles().end()) {
			setDeFactoLiege(liegeItr->second);
		}
	}
}


bool CK3::Title::duchyContainsProvince(const unsigned long long provinceID) const {
	if (rank != TitleRank::duchy)
		return false;

	return std::ranges::any_of(deJureVassals, [&](const auto& deJureVassalItr) {
		return deJureVassalItr.second->rank == TitleRank::county && deJureVassalItr.second->countyProvinces.contains(provinceID);
	});
}