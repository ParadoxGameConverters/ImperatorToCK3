using commonItems;
using ImperatorToCK3.Imperator.Characters;
using System;

namespace ImperatorToCK3.Imperator.Jobs;

public class OfficeJob {
	public ulong CountryId { get; }
	public Character Character { get; }
	public Date StartDate { get; private set; } = new(1, 1, 1);
	public string OfficeType { get; }

	public OfficeJob(BufferedReader reader, CharacterCollection irCharacters) {
		ulong? countryId = null;
		ulong? characterId = null;
		string? officeType = null;

		var parser = new Parser();
		parser.RegisterKeyword("who", r => countryId = r.GetULong());
		parser.RegisterKeyword("character", r => characterId = r.GetULong());
		parser.RegisterKeyword("start_date", r => StartDate = new Date(r.GetString(), AUC: true));
		parser.RegisterKeyword("office", r => officeType = r.GetString());
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseStream(reader);

		CountryId = countryId ?? throw new FormatException("Country ID missing!");
		Character = irCharacters[characterId ?? throw new FormatException("Character ID missing!")];
		OfficeType = officeType ?? throw new FormatException("Office type missing!");
	}
}