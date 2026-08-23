using DotLiquid;
using ImperatorToCK3.Mappers.Technology;
using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Technology;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class InnovationMapperTests {
	[Fact]
	public void GetInnovations_ReturnsMappedInnovations() {
		var mapper = new InnovationMapper();
		var tempFile = Path.Combine(Path.GetTempPath(), $"innovation_mapping_{Guid.NewGuid()}.txt");
		try {
			File.WriteAllText(tempFile, "link = { ir = inv1 ck3 = ck3_a }\nlink = { ir = inv2 ck3 = ck3_b }");
			mapper.LoadLinksAndBonuses(tempFile, new Hash());

			var innovations = mapper.GetInnovations(new[] { "inv1", "inv2" }.ToFrozenSet());

			Assert.Equal(new[] { "ck3_a", "ck3_b" }, innovations);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void GetInnovationProgresses_UsesHighestProgressForSameInnovation() {
		var mapper = new InnovationMapper();
		var tempFile = Path.Combine(Path.GetTempPath(), $"innovation_progress_{Guid.NewGuid()}.txt");
		try {
			File.WriteAllText(
				tempFile,
				"bonus = { ir = inv1 ck3 = ck3_a }\n" +
				"bonus = { ir = inv2 ck3 = ck3_a }\n" +
				"bonus = { ir = inv1 ir = inv2 ck3 = ck3_b }\n"
			);
			mapper.LoadLinksAndBonuses(tempFile, new Hash());

			var progresses = mapper.GetInnovationProgresses(new[] { "inv1", "inv2" }.ToFrozenSet());

			// ck3_a gets 25 for inv1 and 25 for inv2 (from two separate bonuses) but should keep the highest (25).
			// ck3_b gets 50 because both inv1 and inv2 match the same bonus.
			Assert.Equal((ushort)25, progresses["ck3_a"]);
			Assert.Equal((ushort)50, progresses["ck3_b"]);
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void RemoveMappingsWithInvalidInnovations_RemovesInvalidAndUnlistedMappings() {
		var mapper = new InnovationMapper();
		var tempFile = Path.Combine(Path.GetTempPath(), $"innovation_remove_{Guid.NewGuid()}.txt");
		try {
			File.WriteAllText(
				tempFile,
				"link = { ir = inv1 ck3 = ck3_a }\n" +
				"link = { ir = inv2 }\n" +
				"bonus = { ir = inv1 ck3 = ck3_a }\n" +
				"bonus = { ir = inv3 ck3 = ck3_c }\n" +
				"bonus = { ir = inv4 }\n"
			);
			mapper.LoadLinksAndBonuses(tempFile, new Hash());

			mapper.RemoveMappingsWithInvalidInnovations(new HashSet<string> { "ck3_a" });

			// Invalid mappings should be removed.
			Assert.Empty(mapper.GetInnovations(new[] { "inv2" }.ToFrozenSet()));
			Assert.Empty(mapper.GetInnovations(new[] { "inv3" }.ToFrozenSet()));

			// ck3_a should still be mapped in both links and bonuses
			var innovations = mapper.GetInnovations(new[] { "inv1" }.ToFrozenSet());
			Assert.Equal(new[] { "ck3_a" }, innovations);
			var progresses = mapper.GetInnovationProgresses(new[] { "inv1" }.ToFrozenSet());
			Assert.Equal((ushort)25, progresses["ck3_a"]);
		} finally {
			File.Delete(tempFile);
		}
	}
}
