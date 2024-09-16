using commonItems;

namespace ImperatorToCK3.CK3.Characters;

public sealed class Pregnancy {
	public string FatherId { get; init; }
	public string MotherId { get; init; }
	public Date BirthDate { get; init; }
	public Date EstimatedConceptionDate => BirthDate.ChangeByDays(-280);
	public bool IsBastard { get; init; }

	public Pregnancy(string fatherId, string motherId, Date birthDate, bool isBastard) {
		FatherId = fatherId;
		MotherId = motherId;
		BirthDate = birthDate;
		IsBastard = isBastard;
	}
}