namespace ImperatorToCK3.CommonUtils;

public class FieldValue {
	public object? Value { get; set; }
	public string Setter { get; set; } // setter that was used to set the value
	public FieldValue(object? value, string setter) {
		Value = value;
		Setter = setter;
	}
}
