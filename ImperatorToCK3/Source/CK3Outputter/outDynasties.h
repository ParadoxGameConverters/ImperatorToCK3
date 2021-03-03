#ifndef CK3_OUT_DYNASTIES_H
#define CK3_OUT_DYNASTIES_H



#include <map>
#include <memory>
#include <string>



namespace CK3 {

class Dynasty;
void outputDynasties(const std::string& outputModName, const std::map<std::string, std::shared_ptr<Dynasty>>& dynasties);

}



#endif // CK3_OUT_DYNASTIES_H