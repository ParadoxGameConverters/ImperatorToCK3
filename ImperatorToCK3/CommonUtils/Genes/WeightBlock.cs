using commonItems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils.Genes;

public sealed class WeightBlock {
	public uint SumOfAbsoluteWeights { get; private set; } = 0;
	private readonly List<KeyValuePair<string, uint>> objectsList = new();

	public WeightBlock() { }
	public WeightBlock(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterRegex(CommonRegexes.Integer, (reader, absoluteWeightStr) => {
			var newObjectName = reader.GetString();
			if (uint.TryParse(absoluteWeightStr, out var weight)) {
				AddObject(newObjectName, weight);
			} else {
				Logger.Error($"Could not parse absolute weight: {absoluteWeightStr}");
			}
		});
		parser.IgnoreAndLogUnregisteredItems();
	}
	public uint GetAbsoluteWeight(string objectName) {
		foreach (var (key, value) in objectsList) {
			if (key == objectName) {
				return value;
			}
		}
		return 0;
	}
	public double? GetMatchingPercentage(string objectName) {
		uint sumOfPrecedingAbsoluteWeights = 0;
		foreach (var (key, value) in objectsList) {
			if (key == objectName) {
				return (double)sumOfPrecedingAbsoluteWeights / SumOfAbsoluteWeights;
			}
			sumOfPrecedingAbsoluteWeights += value;
		}
		return null;
	}

	public byte? GetSliderValueForObject(string objectName) {
		double? percentage = GetMatchingPercentage(objectName);
		if (percentage is null) {
			return null;
		}
		return (byte)Math.Ceiling(percentage.Value * 255);
	}
	public string? GetMatchingObject(double percentAsDecimal) { // argument must be in range <0; 1>
		if (percentAsDecimal < 0 || percentAsDecimal > 1) {
			throw new ArgumentOutOfRangeException($"percentAsDecimal is {percentAsDecimal}, should be >=0 and <=1");
		}
		uint sumOfPrecedingAbsoluteWeights = 0;
		foreach (var (key, value) in objectsList) {
			sumOfPrecedingAbsoluteWeights += value;
			var maxEntryPercent = (double)sumOfPrecedingAbsoluteWeights / SumOfAbsoluteWeights;
			if (sumOfPrecedingAbsoluteWeights > 0 && percentAsDecimal <= maxEntryPercent) {
				return key;
			}
		}
		return null;
	}
	public void AddObject(string objectName, uint absoluteWeight) {
		objectsList.Add(new KeyValuePair<string, uint>(objectName, absoluteWeight));
		SumOfAbsoluteWeights += absoluteWeight;
	}
	public bool ContainsObject(string objectName) {
		return objectsList.Any(entry => entry.Key == objectName);
	}
	
	public IEnumerable<string> ObjectNames => objectsList.Select(entry => entry.Key);
}