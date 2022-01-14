using commonItems;

namespace ImperatorToCK3.CommonUtils.Genes;

public class Gene {
	public uint? Index { get; internal set; }
	public PDXBool Inheritable { get; internal set; } = new(true);
}