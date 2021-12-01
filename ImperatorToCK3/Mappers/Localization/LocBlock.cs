namespace ImperatorToCK3.Mappers.Localization {
	public class LocBlock {
		public string english = "";
		public string french = "";
		public string german = "";
		public string russian = "";
		public string simp_chinese = "";
		public string spanish = "";

		public LocBlock() { }

		public LocBlock(string englishLoc) {
			english = englishLoc;
			FillMissingLocsWithEnglish();
		}
		public LocBlock(LocBlock otherLocBlock) {
			english = otherLocBlock.english;
			french = otherLocBlock.french;
			german = otherLocBlock.german;
			russian = otherLocBlock.russian;
			simp_chinese = otherLocBlock.simp_chinese;
			spanish = otherLocBlock.spanish;
		}

		// ModifyForEveryLanguage helps remove boilerplate by applying modifyingMethod to every language in the struct
		//
		// For example:
		// nameLocBlock.english = nameLocBlock.english.Replace("$ADJ$", baseAdjLocBlock.english);
		// nameLocBlock.french = nameLocBlock.french.Replace("$ADJ$", baseAdjLocBlock.french);
		// nameLocBlock.german = nameLocBlock.german.Replace("$ADJ$", baseAdjLocBlock.german);
		// nameLocBlock.russian = nameLocBlock.russian.Replace("$ADJ$", baseAdjLocBlock.russian);
		// nameLocBlock.simp_chinese = nameLocBlock.simp_chinese.Replace("$ADJ$", baseAdjLocBlock.simp_chinese);
		// nameLocBlock.spanish = nameLocBlock.spanish.Replace("$ADJ$", baseAdjLocBlock.spanish);
		//
		// Can be replaced by:
		// nameLocBlock.ModifyForEveryLanguage(baseAdjLocBlock, (ref string baseLoc, string modifyingLoc) => {
		//     baseLoc = baseLoc.Replace("$ADJ$", modifyingLoc);
		// });
		public void ModifyForEveryLanguage(LocBlock otherLocBlock, LocDelegate modifyingMethod) {
			modifyingMethod(ref english, otherLocBlock.english);
			modifyingMethod(ref french, otherLocBlock.french);
			modifyingMethod(ref german, otherLocBlock.german);
			modifyingMethod(ref russian, otherLocBlock.russian);
			modifyingMethod(ref simp_chinese, otherLocBlock.simp_chinese);
			modifyingMethod(ref spanish, otherLocBlock.spanish);
		}
		public void SetLocForLanguage(string languageName, string value) {
			switch (languageName) {
				case "english":
					english = value;
					break;
				case "french":
					french = value;
					break;
				case "german":
					german = value;
					break;
				case "russian":
					russian = value;
					break;
				case "simp_chinese":
					simp_chinese = value;
					break;
				case "spanish":
					spanish = value;
					break;
			}
		}
		private void FillMissingLocWithEnglish(ref string language) {
			if (string.IsNullOrEmpty(language)) {
				language = english;
			}
		}
		public void FillMissingLocsWithEnglish() {
			FillMissingLocWithEnglish(ref french);
			FillMissingLocWithEnglish(ref german);
			FillMissingLocWithEnglish(ref russian);
			FillMissingLocWithEnglish(ref simp_chinese);
			FillMissingLocWithEnglish(ref spanish);
		}
	}
}
