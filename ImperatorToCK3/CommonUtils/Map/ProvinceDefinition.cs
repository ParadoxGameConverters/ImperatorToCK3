using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils.Map;

internal sealed class ProvinceDefinition(ulong id) : IIdentifiable<ulong> {
	public ulong Id { get; } = id;
	private readonly HashSet<SpecialProvinceCategory> specialCategories = [];

	internal void AddSpecialCategory(SpecialProvinceCategory category) {
		specialCategories.Add(category);
	}

	internal bool IsColorableImpassable => specialCategories.Contains(SpecialProvinceCategory.ColorableImpassable);
	internal bool IsImpassable => specialCategories.Contains(SpecialProvinceCategory.NonColorableImpassable) ||
	                              specialCategories.Contains(SpecialProvinceCategory.ColorableImpassable);
	//internal bool IsWasteland => IsImpassable || specialCategories.Contains(SpecialProvinceCategory.Uninhabitable); // uncomment if needed
	internal bool IsStaticWater => specialCategories.Contains(SpecialProvinceCategory.StaticWater);
	internal bool IsRiver => specialCategories.Contains(SpecialProvinceCategory.River);
	internal bool IsLand => (!IsStaticWater && !IsRiver) || IsColorableImpassable; // handles provinces 1107 and 1108 being both impassable_mountains and lakes as of CK3 1.17.1
}