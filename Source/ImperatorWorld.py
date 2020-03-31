from Parser.PyParser import registerKeyword, parseFile
from Parser.ParserHelpers import getSingleString, getStringList

DateTextOutput = []
DLCsOutput = []
ModsOutput = []


def getDateStringInList(unused, theStream):
    DateTextOutput.append(getSingleString(theStream))  # DateTextOutput[0] is a date in a string


def getDLCsListInList(unused, theStream):
    DLCsOutput.append(getStringList(theStream))  # DLCsOutput[0] is a list of DLC strings


def getModsListInList(unused, theStream):
    ModsOutput.append(getStringList(theStream))  # ModsOutput[0] is a list of mod strings


def World(ImperatorSaveFileName):
    print("*** Hello Imperator, loading ImperatorWorld. ***")
    registerKeyword('date', getDateStringInList)
    registerKeyword('enabled_dlcs', getDLCsListInList)
    registerKeyword('enabled_mods', getModsListInList)

    print("Importing Imperator save.")
    parseFile(ImperatorSaveFileName)

    print('Date is ' + DateTextOutput[0])  # debug
    print('Activated DLCs:', DLCsOutput[0])  # debug
    print('Activated mods:', ModsOutput[0])  # debug
