#ifndef CK3_OUT_COAS_H
#define CK3_OUT_COAS_H



#include <string>
#include <map>
#include <memory>



namespace CK3 {

class Title;
void outputCoas(const std::string& outputModName, const std::map<std::string, std::shared_ptr<Title>>& titles);

}



#endif // CK3_OUT_COAS_H