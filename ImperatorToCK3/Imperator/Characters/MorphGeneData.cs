namespace ImperatorToCK3.Imperator.Characters; 

public readonly struct MorphGeneData {
	public required string TemplateName { get; init; }
	public required byte Value { get; init; }
	public required string TemplateRecessiveName { get; init; }
	public required byte ValueRecessive { get; init; }
}