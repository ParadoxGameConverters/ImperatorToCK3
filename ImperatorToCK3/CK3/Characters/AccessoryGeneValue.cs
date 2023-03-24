namespace ImperatorToCK3.CK3.Characters; 

public readonly struct AccessoryGeneValue {
	public required string TemplateName { get; init; }
	public required byte IntSliderValue { get; init; }
	public required string TemplateRecessiveName { get; init; }
	public required byte IntSliderValueRecessive { get; init; }

	public override string ToString() {
		return $"\"{TemplateName}\" {IntSliderValue} \"{TemplateRecessiveName}\" {IntSliderValueRecessive}";
	}
}