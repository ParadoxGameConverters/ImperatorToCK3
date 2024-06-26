using commonItems;

namespace ImperatorToCK3.Imperator.Diplomacy;

public sealed class Dependency(ulong overlordId, ulong subjectId, Date startDate, string subjectType) {
	public ulong OverlordId { get; } = overlordId;
	public ulong SubjectId { get; } = subjectId;
	public Date StartDate { get; } = startDate;
	public string SubjectType { get; } = subjectType;
	
	// TODO: don't convert tributaries as vassals
	// TODO: use Imperator subject type definitions to determine how the subject should be treated in CK3 (contracts and obligations)
}