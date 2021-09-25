using commonItems;

namespace ImperatorToCK3.CK3.Provinces {
	public class ProvincePosition {
		public ulong ID;
		public double X;
		public double Y;
		public static ProvincePosition Parse(BufferedReader reader) {
			positionToReturn = new();
			parser.ParseStream(reader);
			return positionToReturn;
		}
		static ProvincePosition() {
			parser.RegisterRegex("id", reader =>
				positionToReturn.ID = ParserHelpers.GetULong(reader)
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
