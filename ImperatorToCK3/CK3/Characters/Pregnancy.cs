using commonItems;

namespace ImperatorToCK3.CK3.Characters; 

public class Pregnancy {
	public string FatherId { get; init; }
	public string MotherId { get; init; }
	public Date BirthDate { get; init; }
	public Date EstimatedConceptionDate => BirthDate.ChangeByDays(-280);
}