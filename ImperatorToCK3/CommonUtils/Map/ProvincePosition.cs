using commonItems;

namespace ImperatorToCK3.CommonUtils.Map;

public sealed class ProvincePosition {
	public ulong Id { get; private set; }
	public double X { get; private set; }
	public double Y { get; private set; }
	public static ProvincePosition Parse(BufferedReader reader) {
		positionToReturn = new ProvincePosition();
		parser.ParseStream(reader);
		return positionToReturn;
	}
	static ProvincePosition() {
		parser.RegisterKeyword("id", reader =>
			positionToReturn.Id = reader.GetULong()
		);
		parser.RegisterKeyword("position", reader => {
			var positionsList = reader.GetDoubles();
			positionToReturn.X = positionsList[0];
			positionToReturn.Y = positionsList[2];
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
	}
	private static ProvincePosition positionToReturn = new();
	private static readonly Parser parser = new();
}