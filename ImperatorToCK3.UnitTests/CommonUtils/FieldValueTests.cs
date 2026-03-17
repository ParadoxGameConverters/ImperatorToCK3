using System;
using System.Collections.Generic;
using System.IO;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils;

public class FieldValueTests {
	[Fact]
	public void Add_AddsToOrderedSet() {
		var set = new OrderedSet<string>();
		var field = new FieldValue(set, "setter");

		field.Add("value");

		Assert.Contains("value", (IEnumerable<string>)set);
	}

	[Fact]
	public void Remove_RemovesFromOrderedSet() {
		var set = new OrderedSet<string> { "value" };
		var field = new FieldValue(set, "setter");

		field.Remove("value");

		Assert.DoesNotContain("value", (IEnumerable<string>)set);
	}

	[Fact]
	public void Add_LogsWarning_OnNonAdditiveValue() {
		var originalOut = Console.Out;
		try {
			var output = new StringWriter();
			Console.SetOut(output);

			var field = new FieldValue("not a set", "setter");
			field.Add("value");

			Assert.Contains("Cannot additively add value", output.ToString());
		} finally {
			Console.SetOut(originalOut);
		}
	}

	[Fact]
	public void Remove_LogsWarning_OnNonAdditiveValue() {
		var originalOut = Console.Out;
		try {
			var output = new StringWriter();
			Console.SetOut(output);

			var field = new FieldValue("not a set", "setter");
			field.Remove("value");

			Assert.Contains("Cannot additively remove value", output.ToString());
		} finally {
			Console.SetOut(originalOut);
		}
	}
}
