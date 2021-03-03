#include "Dynasty.h"
#include "CK3/Character/CK3Character.h"
#include "Imperator/Families/Family.h"
#include "Imperator/Characters/Character.h"
#include "Log.h"



CK3::Dynasty::Dynasty(const std::shared_ptr<Imperator::Family>& impFamily, const mappers::LocalizationMapper& locMapper) {
	const auto& impMembers = impFamily->getMembers();

	if (!impMembers.empty()) {
		// set culture
		auto impHead = impMembers[0].second;
		culture = impHead->getCK3Character()->culture;
	}
	else {
		Log(LogLevel::Warning) << "Couldn't determine culture for dynasty from Imperator family " << impFamily->getID() << " " << impFamily->getKey() << ", needs manual setting!";
	}

	ID = "dynn_IMPTOCK3_" + std::to_string(impFamily->getID());
	name = ID;

	for (const auto& [memberID, member] : impMembers) {
		if (const auto& ck3Member = member->getCK3Character()) {
			ck3Member->setDynastyID(ID);
		}
	}

	const auto& impFamilyLocKey = impFamily->getKey();
	auto impFamilyLoc = locMapper.getLocBlockForKey(impFamilyLocKey);
	if (impFamilyLoc) {
		localization = std::pair(name, *impFamilyLoc);
	}
	else { // fallback: use unlocalized Imperator family key
		localization = std::pair(name, mappers::LocBlock{ impFamilyLocKey,impFamilyLocKey,impFamilyLocKey,impFamilyLocKey,impFamilyLocKey });
	}
}
