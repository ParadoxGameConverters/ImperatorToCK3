from PyParser import registerKeyword, parseFile
from ParserHelpers import getSingleString, getStringList

unused = ''

DateTextOutput = []
def getDateText(dateText, theStream):
    DateTextOutput.append('')  # DateTextOutput[0] empty, dateText is not used
    DateTextOutput.append(getSingleString(theStream))  # so DateTextOutput[1] is dateString

DLCOutput = []
def getDLCOutput(enabled_dlcs, theStream):
    DLCOutput.append('')  # DLCOutput[0] empty, enabled_dlcs is not used
    DLCOutput.append(getStringList(theStream))  # so DLCOutput[1] is a list of DLC strings

def World(ImperatorSaveFileName):
    print("*** Hello Imperator, loading ImperatorWorld. ***")
    # PyParser.registerKeyword('save_game_version', fun1())
    # PyParser.registerKeyword(o'versin', fun1())

    registerKeyword('date', getDateText)
    registerKeyword('enabled_dlcs', getDLCOutput)

    print("Importing Imperator save.")

    parseFile(ImperatorSaveFileName)

    print(DateTextOutput[1])  # debug
    print(DLCOutput[1])  # debug
