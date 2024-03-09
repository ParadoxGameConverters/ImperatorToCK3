using ImperatorToCK3.CommonUtils.Genes;
using System;

namespace ImperatorToCK3.CK3.Characters;

public readonly struct DNAAccessoryGeneValue(
	string templateName,
	string objectName,
	WeightBlock weightBlock,
	string templateRecessiveName,
	string objectRecessiveName,
	WeightBlock weightBlockRecessive
) {
	public DNAAccessoryGeneValue(
		string templateName,
		string objectName,
		WeightBlock weightBlock
	) : this(templateName, objectName, weightBlock, templateName, objectName, weightBlock) { }
	
	public string TemplateName { get; } = templateName;
	public string ObjectName { get; } = objectName;
	public byte IntSliderValue => weightBlock.GetSliderValueForObject(ObjectName);

	public string TemplateRecessiveName { get; } = templateRecessiveName;
	public string ObjectRecessiveName { get; } = objectRecessiveName;
	public byte IntSliderValueRecessive => weightBlockRecessive.GetSliderValueForObject(ObjectRecessiveName);

	public override string ToString() {
		return $"\"{TemplateName}\" {IntSliderValue} \"{TemplateRecessiveName}\" {IntSliderValueRecessive}";
	}

	public bool Equals(DNAGeneValue other) {
		return TemplateName == other.TemplateName && IntSliderValue == other.IntSliderValue && TemplateRecessiveName == other.TemplateRecessiveName && IntSliderValueRecessive == other.IntSliderValueRecessive;
	}

	public override bool Equals(object? obj) {
		return obj is DNAGeneValue other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(TemplateName, IntSliderValue, TemplateRecessiveName, IntSliderValueRecessive);
	}

	public static bool operator ==(DNAAccessoryGeneValue left, DNAAccessoryGeneValue right) {
		return left.Equals(right);
	}

	public static bool operator !=(DNAAccessoryGeneValue left, DNAAccessoryGeneValue right) {
		return !(left == right);
	}
}