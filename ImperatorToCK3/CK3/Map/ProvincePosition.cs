using commonItems;

namespace ImperatorToCK3.CK3.Map {
	public class ProvincePosition {
		public ulong Id;
		public double X;
		public double Y;
		public static ProvincePosition Parse(BufferedReader reader) {
			positionToReturn = new ProvincePosition();
			parser.ParseStream(reader);
			return positionToReturn;
		}
		static ProvincePosition() {
			parser.RegisterRegex("id", reader =>
				positionToReturn.Id = ParserHelpers.GetULong(reader)
			);
			parser.RegisterKeyword("position", reader => {
				var positionsList = ParserHelpers.GetDoubles(reader);
				positionToReturn.X = positionsList[0];
				positionToReturn.Y = positionsList[2];
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		}
		private static ProvincePosition positionToReturn = new();
		private static readonly Parser parser = new();
	}
}
