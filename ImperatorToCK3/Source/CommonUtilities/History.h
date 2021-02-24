#ifndef HISTORY_H
#define HISTORY_H


#include "Parser.h"
#include "Date.h"
#include <map>
#include <set>

struct uniqueValueField
{
	std::string field;
	std::string setter;
};
struct flagField
{
	std::string field;
	std::string setter;
	std::string unsetter;
};
struct containerField
{
	std::string field;
	std::string inserter;
	std::string remover;
};


class FieldHistory: public commonItems::parser
{
public:
	FieldHistory() = default;
	explicit FieldHistory(std::istream& theStream);
	[[nodiscard]] std::optional<std::string> getValue(const date& date);
private:
	void registerKeys();
	std::map<date, std::string> valueHistory;
}; // class DatedHistoryEntry


class History: public commonItems::parser
{
public:
	explicit History(std::istream& theStream, const std::set<std::string>& fields);
	[[nodiscard]] std::optional<std::string> getValue(const std::string& fieldName, const date& date) const;
private:
	std::map<std::string, FieldHistory> fields;
};

#endif // HISTORY_H