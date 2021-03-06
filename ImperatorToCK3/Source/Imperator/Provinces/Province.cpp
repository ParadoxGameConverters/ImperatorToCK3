#include "Province.h"
#include "Imperator/Countries/Country.h"
#include "Log.h"



void Imperator::Province::linkOwnerCountry(const std::shared_ptr<Country>& country) {
	if (!country) {
		Log(LogLevel::Warning) << "Province " << ID << ": cannot link nullptr country!";
		return;
	}
	if (country->getID() != ownerCountry.first)
		Log(LogLevel::Warning) << "Province " << ID << ": cannot link country " << country->getID() << ": wrong ID!";
	else {
		ownerCountry.second = country;
	}
}
