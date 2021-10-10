using commonItems;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Genes {
	public class WeightBlock : Parser {
		public uint SumOfAbsoluteWeights { get; private set; } = 0;
		private readonly List<KeyValuePair<string, uint>> objectsList = new();

		public WeightBlock() { }
		public WeightBlock(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterRegex(CommonRegexes.Integer, (reader, absoluteWeightStr) => {
				var newObjectName = ParserHelpers.GetString(reader);
				if (uint.TryParse(absoluteWeightStr, out var weight)) {
					AddObject(newObjectName, weight);
				} else {
					Logger.Error("Could not parse absolute weight: " + absoluteWeightStr);
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public uint GetAbsoluteWeight(string objectName) {
			foreach (var (key, value) in objectsList) {
				if (key == objectName) {
					return value;
				}
			}
			return 0;
		}
		public double GetMatchingPercentage(string objectName) {
			uint sumOfPrecedingAbsoluteWeights = 0;
			foreach (var (key, value) in objectsList) {
				if (key == objectName) {
					return (double)sumOfPrecedingAbsoluteWeights / SumOfAbsoluteWeights;
				}
				sumOfPrecedingAbsoluteWeights += value;
			}
			throw new KeyNotFoundException($"Set entry {objectName} not found!");
		}
		public string? GetMatchingObject(double percentAsDecimal) { // argument must be in range <0; 1>
			if (percentAsDecimal < 0 || percentAsDecimal > 1) {
				throw new ArgumentOutOfRangeException("percentAsDecimal is " + percentAsDecimal + ", should be in range <0;1>");
			}
			uint sumOfPrecedingAbsoluteWeights = 0;
			foreach (var (key, value) in objectsList) {
				sumOfPrecedingAbsoluteWeights += value;
				if (sumOfPrecedingAbsoluteWeights > 0 && percentAsDecimal <= (double)sumOfPrecedingAbsoluteWeights / SumOfAbsoluteWeights) {
					return key;
				}
			}
			return null;
		}
		public void AddObject(string objectName, uint absoluteWeight) {
			objectsList.Add(new KeyValuePair<string, uint>(objectName, absoluteWeight));
			SumOfAbsoluteWeights += absoluteWeight;
		}
	}
}
