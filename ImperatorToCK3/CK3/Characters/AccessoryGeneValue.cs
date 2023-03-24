namespace ImperatorToCK3.CK3.Characters; 

public readonly struct AccessoryGeneValue {
	public string TemplateName { get; init; }
	public byte IntSliderValue { get; init; }
	public string TemplateRecessiveName { get; init; }
	public byte IntSliderValueRecessive { get; init; }

	public override string ToString() {
		return $"\"{TemplateName}\" {IntSliderValue} \"{TemplateRecessiveName}\" {IntSliderValueRecessive}";
	}
}