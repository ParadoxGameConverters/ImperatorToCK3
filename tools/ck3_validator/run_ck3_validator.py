# Validates a generated CK3 mod with the LT-Rascek/CK3_Validator toolsuite
# (https://github.com/LT-Rascek/CK3_Validator, CC0).
#
# Two upstream assumptions break on Windows, so this wrapper adapts them:
# 1. ck3_common_utils.compare_file_path_with_item() splits paths on '/' and
#    anchors its folder regexes; os.walk yields backslash paths on Windows,
#    so every file would be silently skipped and checks would pass vacuously.
#    The walker in build_normalized_search() is a separator-normalized
#    reimplementation.
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
	# Byte-level equivalent of the upstream encoding check's action():
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


def build_normalized_search(compare_file_path_with_item, task_progress_meter):
	# Adapted from ck3_common_utils.search_over_mod_structure with path
	# separators normalized to '/' so upstream folder matching works on Windows.
	def search_over_mod_structure(root_dir, file_keyword, file_action_object, data_object,
			console_output,
			database=("common", "events", "history"),
			check_localization=False):
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

	return search_over_mod_structure


def import_upstream_modules(validator_scripts_dir):
	sys.path.insert(0, validator_scripts_dir)
	import check_localization_file_endings as loc_endings
	import ck3_common_utils
	from ck3_common_utils import compare_file_path_with_item, task_progress_meter

	# Rebind the normalized walker everywhere the upstream modules bound the original one.
	search = build_normalized_search(compare_file_path_with_item, task_progress_meter)
	ck3_common_utils.search_over_mod_structure = search
	loc_endings.search_over_mod_structure = search

	return loc_endings, search


def run_encoding_check(mod_path, search):
	# Every *.yml in the whole mod tree must be UTF-8-BOM encoded.
	# (The upstream run_test() hardcodes database=['common', 'events', 'history'],
	# which skips localization/, so it is not reused here.)
	improperly_encoded = [
		f
		for f in search(
				mod_path, r"\.yml$", Utf8BomCheck(), [],
				console_output=False, database=None, check_localization=True)
		if f
	]
	if improperly_encoded:
		print("Improperly encoded files (missing UTF-8 BOM):")
		for file in improperly_encoded:
			print(f"	{file}")
		return True

	print(f"Encoding check passed ({sum(1 for _ in iter_yml_files(mod_path))} yml files checked)")
	return False


def run_localization_endings_check(loc_endings, mod_path):
	# Localization file names must end with _l_<language>.yml.
	if loc_endings.run_test(mod_path, ".+", exceptions_fname="", console_output=False):
		return True

	print("Localization endings check passed")
	return False


def iter_yml_files(root_dir):
	for directory, _, _ in os.walk(root_dir):
		yield from glob.glob(os.path.join(directory, "*.yml"))


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

	errors_found = False
	try:
		loc_endings, search = import_upstream_modules(validator_scripts_dir)
	except ImportError as e:
		print(f"Failed to import CK3_Validator modules from {validator_scripts_dir}: {e}")
		return 1

	errors_found |= run_encoding_check(mod_path, search)
	errors_found |= run_localization_endings_check(loc_endings, mod_path)

	if errors_found:
		print("CK3 Validator: validation failed!")
		return 1
	print("CK3 Validator: successfully validated!")
	return 0


if __name__ == "__main__":
	sys.exit(main())
