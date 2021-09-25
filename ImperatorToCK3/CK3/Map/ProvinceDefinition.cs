using ImageMagick;

namespace ImperatorToCK3.CK3.Map {
	public class ProvinceDefinition {
		public ulong ID { get; }
		public MagickColor Color { get; }
		public ProvinceDefinition(ulong id, byte r, byte g, byte b) {
			ID = id;
			Color = MagickColor.FromRgb(r, g, b);
		}
	}
}
