from PyParser import registerKeyword, parseFile
from ParserHelpers import getSingleString, getStringList

unused = ''

DateTextOutput = []
def getDateText(unused, theStream):
    DateTextOutput.append('')  # DateTextOutput[0] empty, keyword is not used
    DateTextOutput.append(getSingleString(theStream))  # so DateTextOutput[1] is dateString

DLCsOutput = []
def getDLCsOutput(unused, theStream):
    DLCsOutput.append('')  # DLCsOutput[0] empty, keyword is not used
    DLCsOutput.append(getStringList(theStream))  # so DLCsOutput[1] is a list of DLC strings

ModsOutput = []
def getModsOutput(unused, theStream):
    ModsOutput.append('')  # ModsOutput[0] empty, keyword is not used
    ModsOutput.append(getStringList(theStream))  # so ModsOutput[1] is a list of DLC strings

def World(ImperatorSaveFileName):
    print("*** Hello Imperator, loading ImperatorWorld. ***")
    # PyParser.registerKeyword('save_game_version', fun1())
    # PyParser.registerKeyword(o'versin', fun1())

    registerKeyword('date', getDateText)
    registerKeyword('enabled_dlcs', getDLCsOutput)
    registerKeyword('enabled_mods', getModsOutput)

    print("Importing Imperator save.")

    parseFile(ImperatorSaveFileName)

    print(DateTextOutput[1])  # debug
    print(DLCsOutput[1])  # debug
    print(ModsOutput[1])  # debug
