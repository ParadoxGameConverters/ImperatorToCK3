#include "Family.h"
#include "Imperator/Characters/Character.h"


void Imperator::Family::linkMember(const std::shared_ptr<Character>& newMemberPtr) {
	for (auto& [memberID, memberPtr] : members) {
		if (memberID == newMemberPtr->getID()) {
			memberPtr = newMemberPtr;
			break;
		}
	}
}
