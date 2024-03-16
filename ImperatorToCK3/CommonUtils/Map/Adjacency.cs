using CsvHelper.Configuration.Attributes;

namespace ImperatorToCK3.CommonUtils.Map;

public sealed class Adjacency {
	[Index(0)] public long From { get; set; }
	[Index(1)]  public long To { get; set; }
	[Index(2)] public string Type { get; set; } = string.Empty;
	[Index(3)] public long Through { get; set; }
	[Index(4)] public long StartX { get; set; }
	[Index(5)] public long StartY { get; set; }
	[Index(6)] public long StopX { get; set; }
	[Index(7)] public long StopY { get; set; }
	[Index(8)] public string Comment { get; set; } = string.Empty;
}