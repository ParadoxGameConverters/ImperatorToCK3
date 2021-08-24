namespace ImperatorToCK3.Imperator.Pops {
    public partial class Pop {
        public ulong ID { get; } = 0;
        public string Type { get; set; } = "";
        public string Culture { get; set; } = "";
        public string Religion { get; set; } = "";
		public Pop(ulong ID) {
			this.ID = ID;
		}
    }
}
