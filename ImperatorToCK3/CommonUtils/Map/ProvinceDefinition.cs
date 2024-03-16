using commonItems.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils.Map;

public sealed class ProvinceDefinition(ulong id) : IIdentifiable<ulong> {
	public ulong Id { get; } = id;
	private readonly HashSet<SpecialProvinceCategory> specialCategories = [];

	public void AddSpecialCategory(SpecialProvinceCategory category) {
		specialCategories.Add(category);
	}

	public bool IsColorableImpassable => specialCategories.Contains(SpecialProvinceCategory.ColorableImpassable);
	public bool IsImpassable => specialCategories.Contains(SpecialProvinceCategory.NonColorableImpassable) ||
	                            specialCategories.Contains(SpecialProvinceCategory.ColorableImpassable);
	public bool IsStaticWater => specialCategories.Contains(SpecialProvinceCategory.StaticWater);
	public bool IsRiver => specialCategories.Contains(SpecialProvinceCategory.River);
	public bool IsLand => !IsStaticWater && !IsRiver;
}