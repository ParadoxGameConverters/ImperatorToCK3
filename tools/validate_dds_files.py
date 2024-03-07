# make sure every file in ImperatorToCK3/Data_Files/blankMod/output/gfx/icons/faith/ is valid: .dds, 100x100 pixels, format: 32bit-A8R8G8B8, no mipmaps
# Based on CK3 mod coop FAQ on DDS files:
# https://discord.com/channels/735413460439007241/948759032326410240/1214691391737823263

import os
from wand.image import Image # installation: pip install Wand

faith_icons_dir = "ImperatorToCK3/Data_Files/blankMod/output/gfx/interface/icons/faith/"
faith_icons = [faith_icons_dir + f for f in os.listdir(faith_icons_dir) if f.endswith(".dds")]

errors_found = False

# check each file
for faith_icon in faith_icons:
    with Image(filename=faith_icon) as img:
        # check if the file is a valid .dds file
        if img.format != 'DDS':
            print(f"Invalid .dds file: {faith_icon}")
            errors_found = True

        # check if the file is 100x100 pixels
        if img.width != 100:
            print(f"Incorrect width: {faith_icon} ({img.width})")
            errors_found = True
        if img.height != 100:
            print(f"Incorrect height: {faith_icon} ({img.height})")
            errors_found = True

        # check if the file is 32bit-A8R8G8B8
        if img.alpha_channel != True:
            print(f"Missing alpha channel: {faith_icon}")
            errors_found = True
        if img.depth != 8:
            print(f"Incorrect depth: {faith_icon} ({img.depth})")
            errors_found = True

if errors_found:
    print("Errors found")
    exit(1)
else:
    print("No errors found")
    exit(0)
