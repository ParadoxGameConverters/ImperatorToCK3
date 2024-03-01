using commonItems;

namespace ImperatorToCK3.Imperator.Diplomacy;

public class Dependency(ulong overlordId, ulong subjectId, Date startDate, string subjectType) {
	public ulong OverlordId { get; } = overlordId;
	public ulong SubjectId { get; } = subjectId;
	public Date StartDate { get; } = startDate;
	public string SubjectType { get; } = subjectType;
	
	// TODO: use SubjectType to determine how the subject should be treated in CK3
}