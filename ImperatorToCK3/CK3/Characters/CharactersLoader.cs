using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.CK3.Characters;

public partial class CharacterCollection {
	public CharacterCollection(string characterHistoryPath, Configuration config) {
		historyFactory.GetHistory(characterHistoryPath, config.CK3Path);
	}

	private readonly HistoryFactory historyFactory = new HistoryFactory.HistoryFactoryBuilder()
		.WithSimpleField("name", "name", null)
		.WithSimpleField("female", "female", null)
		.WithSimpleField("dynasty", "dynasty", null)
		.WithSimpleField("martial", "martial", null)
		.WithSimpleField("diplomacy", "diplomacy", null)
		.WithSimpleField("intrigue", "intrigue", null)
		.WithSimpleField("stewardship", "stewardship", null)
		.WithSimpleField("culture", "culture", null)
		.WithSimpleField("religion", "religion", null)
		.WithAdditiveContainerField("traits", "add_trait", "remove_trait")
		.WithSimpleField("dna", "dna", null)
		.WithSimpleField("mother", "mother", null)
		.WithSimpleField("father", "father", null)

		.Build();
}