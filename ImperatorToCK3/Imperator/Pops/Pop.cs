using commonItems.Collections;

namespace ImperatorToCK3.Imperator.Pops {
	public partial class Pop : IIdentifiable<ulong> {
		public ulong Id { get; } = 0;
		public string Type { get; set; } = "";
		public string Culture { get; set; } = "";
		public string Religion { get; set; } = "";
		public Pop(ulong id) {
			Id = id;
		}
	}
}
