using commonItems;

namespace ImperatorToCK3.Imperator.Characters;

public sealed class Unborn {
	public ulong MotherId { get; private set; }
	public ulong FatherId { get; private set; }
	public Date BirthDate { get; }
	public Date EstimatedConceptionDate => BirthDate.ChangeByDays(-280);
	public bool IsBastard { get; set; } = false;

	public Unborn(ulong motherId, ulong fatherId, Date birthDate, bool isBastard) {
		MotherId = motherId;
		FatherId = fatherId;
		BirthDate = birthDate;
		IsBastard = isBastard;
	}

	public static Unborn? Parse(BufferedReader unbornReader) {
		ulong? motherId = null;
		ulong? fatherId = null;
		Date? birthDate = null;
		bool isBastard = false;

		var parser = new Parser();
		parser.RegisterKeyword("mother", reader => motherId = reader.GetULong());
		parser.RegisterKeyword("father", reader => fatherId = reader.GetULong());
		parser.RegisterKeyword("date", reader => birthDate = new Date(reader.GetString(), AUC: true));
		parser.RegisterKeyword("is_bastard", reader => isBastard = reader.GetBool());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(unbornReader);

		if (motherId is null || fatherId is null || birthDate is null) {
			return null;
		}

		return new Unborn((ulong)motherId, (ulong)fatherId, birthDate, isBastard);
	}
}