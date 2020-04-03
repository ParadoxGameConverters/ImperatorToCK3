from Parser.PyParser import registerKeyword, parseFile
from Parser.ParserHelpers import getSingleString, getStringList
from Log import Log

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
    Log('info', '*** Hello Imperator, loading ImperatorWorld. ***')
    registerKeyword('date', getDateStringInList)
    registerKeyword('enabled_dlcs', getDLCsListInList)
    registerKeyword('enabled_mods', getModsListInList)

    Log('info', 'Importing Imperator save.')
    parseFile(ImperatorSaveFileName)

    Log('debug', 'Date is ' + DateTextOutput[0])
    Log('debug', 'Activated DLCs:' + str(DLCsOutput[0]))
    Log('debug', 'Activated mods:' + str(ModsOutput[0]))
