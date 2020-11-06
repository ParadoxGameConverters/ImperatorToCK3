#ifndef CK3_OUT_TITLES
#define CK3_OUT_TITLES

#include <map>
#include <memory>
#include <string>
#include "../CK3/Titles/Title.h"


namespace CK3
{

	void outputTitles(const std::string& outputModName, const std::string& ck3Path, const std::map<std::string, std::shared_ptr<Title>>& titles);
	void outputTitleHistory(const std::string& outputModName, const std::shared_ptr<Title>& title, std::ofstream& outputStream, std::set<std::string>& alreadyOutputted);
	void outputTitlesHistory(const std::string& outputModName, const std::map<std::string, std::shared_ptr<Title>>& titles);
	
}



#endif // CK3_OUT_TITLES