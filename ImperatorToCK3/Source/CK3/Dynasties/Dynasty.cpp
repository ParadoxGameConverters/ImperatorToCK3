#include "Dynasty.h"
#include "CK3/Character/CK3Character.h"
#include "Imperator/Families/Family.h"
#include "Imperator/Characters/Character.h"
#include "Log.h"



CK3::Dynasty::Dynasty(const std::shared_ptr<Imperator::Family>& impFamily, const mappers::LocalizationMapper& locMapper) {
	const auto& impMembers = impFamily->getMembers();
	if (!impMembers.empty()) {
		auto impHead = impMembers[0].second;
		culture = impHead->getCK3Character()->culture;
	}
	ID = "dynn_IMPTOCK3_" + std::to_string(impFamily->getID());
	name = ID;

	const auto& impFamilyLocKey = impFamily->getKey();
	auto impFamilyLoc = locMapper.getLocBlockForKey(impFamilyLocKey);
	if (impFamilyLoc) {
		localization = std::pair(name, *impFamilyLoc);
	}
	else { // fallback: use unlocalized Imperator family key
		localization = std::pair(name, mappers::LocBlock{ impFamilyLocKey,impFamilyLocKey,impFamilyLocKey,impFamilyLocKey,impFamilyLocKey });
	}
}
