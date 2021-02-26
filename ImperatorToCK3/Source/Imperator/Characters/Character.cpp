#include <utility>
#include "Character.h"
#include "../Families/Family.h"



const std::string& Imperator::Character::getCulture() const
{
	if (!culture.empty())
		return culture;
	if (family.first && !family.second->getCulture().empty())
		return family.second->getCulture();
	return culture;
}

std::string Imperator::Character::getAgeSex() const
{
	if (age >= 16)
	{
		if (female) return "female";
		return "male";
	}
	if (female) return "girl";
	return "boy";
}
