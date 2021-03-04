#ifndef CK3_OUT_PROVINCES_H
#define CK3_OUT_PROVINCES_H



#include <map>
#include <memory>
#include <string>



namespace CK3 {

class Province;
void outputHistoryProvinces(const std::string& outputModName, const std::map<unsigned long long, std::shared_ptr<Province>>& provinces);

}



#endif // CK3_OUT_PROVINCES_H