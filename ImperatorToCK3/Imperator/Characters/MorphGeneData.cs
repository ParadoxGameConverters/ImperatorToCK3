namespace ImperatorToCK3.Imperator.Characters; 

public readonly struct MorphGeneData {
	public string TemplateName { get; init; }
	public byte Value { get; init; }
	public string TemplateRecessiveName { get; init; }
	public byte ValueRecessive { get; init; }
}