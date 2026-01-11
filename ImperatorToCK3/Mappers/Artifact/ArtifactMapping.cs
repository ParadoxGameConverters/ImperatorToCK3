using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Artifact;

internal record ArtifactMapping {
	public string CK3Visual { get; init; } = string.Empty;
	public string CK3Type { get; init; } = string.Empty;

	public List<string> IRTreasureIds { get; init; } = [];
	public List<string> IRIconIds { get; init; } = [];
}