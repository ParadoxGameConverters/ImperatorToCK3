namespace ImperatorToCK3.Imperator.Characters; 

public readonly struct AccessoryGeneData {
	public required string GeneTemplate { get; init; }
	public required string ObjectName { get; init; }
	public required string GeneTemplateRecessive { get; init; }
	public required string ObjectNameRecessive { get; init; }
}