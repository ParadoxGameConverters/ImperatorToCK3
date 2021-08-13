#include "Dynasty.h"
#include "CK3/Character/CK3Character.h"
#include "Imperator/Families/Family.h"
#include "Imperator/Characters/Character.h"
#include "Log.h"
#include <ranges>



CK3::Dynasty::Dynasty(const Imperator::Family& impFamily, const mappers::LocalizationMapper& locMapper) : ID{"dynn_IMPTOCK3_" + std::to_string(impFamily.getID())} {
	name = ID;

	const auto& impMembers = impFamily.getMembers();
	if (!impMembers.empty()) {
		auto firstMember = impMembers[0];
		culture = firstMember.second->getCK3Character()->culture;  // make head's culture the dynasty culture
	}
	else {
		Log(LogLevel::Warning) << "Couldn't determine culture for dynasty " << ID << ", needs manual setting!";
	}

	for (const auto& member : impMembers | std::views::values) {
		if (const auto& ck3Member = member->getCK3Character()) {
			ck3Member->setDynastyID(ID);
		}
	}

	const auto& impFamilyLocKey = impFamily.getKey();
	auto impFamilyLoc = locMapper.getLocBlockForKey(impFamilyLocKey);
	if (impFamilyLoc) {
		localization = std::pair(name, *impFamilyLoc);
	}
	else { // fallback: use unlocalized Imperator family key
		localization = std::pair(name, mappers::LocBlock{impFamilyLocKey,impFamilyLocKey,impFamilyLocKey,impFamilyLocKey,impFamilyLocKey,impFamilyLocKey});
	}
}
