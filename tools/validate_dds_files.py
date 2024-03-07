# make sure every file in ImperatorToCK3/Data_Files/blankMod/output/gfx/icons/faith/ is valid: .dds, 100x100 pixels, format: 32bit-A8R8G8B8, no mipmaps
# Based on CK3 mod coop FAQ on DDS files:
# https://discord.com/channels/735413460439007241/948759032326410240/1214691391737823263

# find all .dds files in the faith icons directory

import os
import subprocess

faith_icons_dir = "ImperatorToCK3/Data_Files/blankMod/output/gfx/interface/icons/faith/"
faith_icons = [faith_icons_dir + f for f in os.listdir(faith_icons_dir) if f.endswith(".dds")]

errors_found = False

# check each file
for faith_icon in faith_icons:
    # check if the file is a valid .dds file
    if subprocess.check_output(f"magick identify -format %m \"{faith_icon}\"", shell=True).decode('utf-8') != 'DDS':
        print(f"Invalid .dds file: {faith_icon}")
        errors_found = True

    # check if the file is 100x100 pixels
    width = subprocess.check_output(f"magick identify -format %w \"{faith_icon}\"", shell=True).decode('utf-8')
    if width != '100':
        print(f"Incorrect width: {faith_icon} ({width})")
        errors_found = True
    height = subprocess.check_output(f"magick identify -format %h \"{faith_icon}\"", shell=True).decode('utf-8')
    if height != '100':
        print(f"Incorrect height: {faith_icon} ({height})")
        errors_found = True

    # check if the file is 32bit-A8R8G8B8
    format = subprocess.check_output(f"magick identify -format %z \"{faith_icon}\"", shell=True).decode('utf-8')
    if format != '8':
        print(f"Incorrect format: {faith_icon} ({format})")
        errors_found = True

    # check if the file has no mipmaps
    identify_output = subprocess.check_output(f"magick identify -verbose \"{faith_icon}\"", shell=True).decode("utf-8")
    has_mipmaps = "mipmap" in identify_output
    if has_mipmaps:
        print(f"Mipmaps found: {faith_icon}")
        errors_found = True

if errors_found:
    print("Errors found")
    exit(1)
else:
    print("No errors found")
    exit(0)
