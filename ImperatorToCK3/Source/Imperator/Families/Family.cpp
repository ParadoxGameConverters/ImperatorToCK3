#include "Family.h"
#include "Imperator/Characters/Character.h"
#include "Log.h"



void Imperator::Family::linkMember(const std::shared_ptr<Character>& newMemberPtr) {
	if (!newMemberPtr) {
		Log(LogLevel::Warning) << "Family " << ID << ": cannot link nullptr member!";
		return;
	}
	for (auto& [memberID, memberPtr] : members) {
		if (memberID == newMemberPtr->getID()) {
			memberPtr = newMemberPtr;
			return;
		}
	}
	if (newMemberPtr->getDeathDate()) { // if character is dead, his ID needs to be added to the map
		members.emplace_back(newMemberPtr->getID(), newMemberPtr);
		return;
	}
	// matching ID was not found
	Log(LogLevel::Warning) << "Family " << ID << ": cannot link " << newMemberPtr->getID() << ": not found in members!";
}
