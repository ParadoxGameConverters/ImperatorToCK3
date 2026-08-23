# Validates a generated CK3 mod with the LT-Rascek/CK3_Validator toolsuite
# (https://github.com/LT-Rascek/CK3_Validator, CC0).
#
# Two upstream assumptions break on Windows, so this wrapper adapts them:
# 1. ck3_common_utils.compare_file_path_with_item() splits paths on '/' and
#    anchors its folder regexes; os.walk yields backslash paths on Windows,
#    so every file would be silently skipped and checks would pass vacuously.
#    The walker below is a separator-normalized reimplementation.
# 2. The upstream encoding check opens files in text mode with the platform
#    locale encoding (e.g. cp1252), misreporting UTF-8-BOM files as broken.
#    A byte-level equivalent is used instead (Utf8BomCheck).
# The encoding check is also restricted to *.yml files: CK3 requires
# UTF-8-BOM only for localization, while the converter emits plain UTF-8 .txt
# files by design.
#
# Usage:
#   python run_ck3_validator.py <mod_folder> <ck3_validator_repo_checkout>

import glob
import os
import re
import sys

BOM = b"\xef\xbb\xbf"


class Utf8BomCheck:
	# Byte-level equivalent of check_encoding_item.CheckFileEncoding.action():
	# reports the file unless it starts with a UTF-8 BOM and decodes as UTF-8.
	def action(self, file):
		with open(file, "rb") as file_obj:
			data = file_obj.read()
		if not data.startswith(BOM):
			return [file]
		try:
			data.decode("utf-8")
		except UnicodeDecodeError:
			return [file]
		return [None]


def main():
	if len(sys.argv) != 3:
		print(f"Usage: python {sys.argv[0]} <mod_folder> <ck3_validator_repo_checkout>")
		return 1

	mod_path = os.path.abspath(sys.argv[1])
	validator_scripts_dir = os.path.join(os.path.abspath(sys.argv[2]), "test_scripts")

	if not os.path.isdir(mod_path):
		print(f"Mod folder not found: {mod_path}")
		return 1
	if not os.path.isdir(validator_scripts_dir):
		print(f"CK3_Validator checkout not found under: {validator_scripts_dir}")
		return 1

	sys.path.insert(0, validator_scripts_dir)
	import check_localization_file_endings as loc_endings
	from ck3_common_utils import compare_file_path_with_item, task_progress_meter

	def search_over_mod_structure(root_dir, file_keyword, file_action_object, data_object,
			console_output,
			database=("common", "events", "history"),
			check_localization=False):
		# Adapted from ck3_common_utils.search_over_mod_structure with path
		# separators normalized to '/' so upstream folder matching works on Windows.
		file_list = [y for x in os.walk(root_dir) for y in glob.glob(os.path.join(x[0], "*.txt"))]
		if check_localization:
			file_list.extend(y for x in os.walk(root_dir) for y in glob.glob(os.path.join(x[0], "*.yml")))
		file_list = [f.replace(os.sep, "/") for f in file_list]
		database_items = "(" + "|".join(database) + ")" if database else ""

		for index, file in enumerate(file_list):
			if console_output:
				task_progress_meter(index, len(file_list))
			if re.search(file_keyword, file) and (not database or compare_file_path_with_item(file, database_items)):
				if isinstance(data_object, list):
					data_object.extend(file_action_object.action(file))
				else:
					data_object = file_action_object.action(file)
		if console_output:
			task_progress_meter(len(file_list), len(file_list))
		return data_object

	# Rebind the patched walker everywhere the upstream modules bound the original one.
	import ck3_common_utils
	ck3_common_utils.search_over_mod_structure = search_over_mod_structure
	loc_endings.search_over_mod_structure = search_over_mod_structure

	errors_found = False

	# Check 1: encoding - every *.yml in the whole mod tree must be UTF-8-BOM.
	# (Upstream's run_test() hardcodes database=['common', 'events', 'history'],
	# which skips localization/, so its checking class is used directly instead.)
	improperly_encoded = [
		f
		for f in search_over_mod_structure(
				mod_path, r"\.yml$", Utf8BomCheck(), [],
				console_output=False, database=None, check_localization=True)
		if f
	]
	if improperly_encoded:
		errors_found = True
		print("Improperly encoded files (missing UTF-8 BOM):")
		for file in improperly_encoded:
			print(f"	{file}")
	else:
		print(f"Encoding check passed ({sum(1 for _ in iter_yml_files(mod_path))} yml files checked)")

	# Check 2: localization file names must end with _l_<language>.yml.
	if loc_endings.run_test(mod_path, ".+", exceptions_fname="", console_output=False):
		errors_found = True
	else:
		print("Localization endings check passed")

	if errors_found:
		print("CK3 Validator: validation failed!")
		return 1
	print("CK3 Validator: successfully validated!")
	return 0


def iter_yml_files(root_dir):
	for directory, _, _ in os.walk(root_dir):
		yield from glob.glob(os.path.join(directory, "*.yml"))


if __name__ == "__main__":
	sys.exit(main())
