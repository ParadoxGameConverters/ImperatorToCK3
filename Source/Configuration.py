from Parser.ParserHelpers import getSingleString
from Parser.PyParser import parseFile, registerKeyword, clearRegisteredKeywords
import os
import sys

ImperatorDirectoryOutput = []
ImperatorDocumentsDirectoryOutput = []
ImperatorSavePathOutput = []
# CK3DirectoryOutput = []
# CK3DocumentsDirectoryOutput = []
ImperatorDeJureOutput = []


def readConfigurationDetails(unused, theStream):
    def verifyImperatorPath(path):
        if not os.path.isdir(path):
            sys.exit(path + ' does not exist!')
        if not os.path.isfile(path + r'\binaries\imperator.exe'):
            sys.exit(path + ' does not contain Imperator: Rome!')
        print('\tImperator: Rome install path is ' + path)

    def getImperatorDirectory(unused, theStream):
        ImperatorDirectoryOutput.append(getSingleString(theStream))
        verifyImperatorPath(ImperatorDirectoryOutput[0])

    registerKeyword('ImperatorDirectory', getImperatorDirectory)

    def verifyImperatorDocumentsPath(path):
        if not os.path.isdir(path):
            sys.exit(path + ' does not exist!')
        print('\tImperator: Rome documents directory path is ' + path)

    def getImperatorDocumentsDirectory(unused, theStream):
        ImperatorDocumentsDirectoryOutput.append(getSingleString(theStream))
        verifyImperatorDocumentsPath(ImperatorDocumentsDirectoryOutput[0])

    registerKeyword('ImperatorDocumentsDirectory', getImperatorDocumentsDirectory)

    def getImperatorSavePath(unused, theStream):
        ImperatorSavePathOutput.append(getSingleString(theStream))

    registerKeyword('ImperatorSavePath', getImperatorSavePath)

    # def verifyCK3Path(path):
    #     if not os.path.isdir(path):
    #         sys.exit(path + ' does not exist!')
    #     print('\tCK3 install path is ' + path)

    # def getCK3Directory(unused, theStream):
    #    CK3DirectoryOutput.append(getSingleString(theStream))
    #     verifyCK3Path(CK3DirectoryOutput[0])

    # registerKeyword('CK3Directory', getCK3Directory)  # TODO #5: enable when CK3 is released

    # def verifyCK3DocumentsPath(path):
    #     if not os.path.isdir(path):
    #         sys.exit(path + ' does not exist!')
    #     print('\tCK3 documents directory path is ' + path)

    # def getCK3DocumentsDirectory(unused, theStream):
    #     CK3DocumentsDirectoryOutput.append(getSingleString(theStream))
    #     verifyCK3DocumentsPath(CK3DocumentsDirectoryOutput[0])

    # registerKeyword('CK3DocumentsDirectory', getCK3DocumentsDirectory)  # TODO #5: enable when CK3 is released

    def getImperatorDeJure(unused, theStream):
        ImperatorDeJureOutput.append(getSingleString(theStream))
        if ImperatorDeJureOutput[0] == 'yes':
            print('\tUsing Imperator: Rome provinces and regions to generate CK3 de iure.')
        else:
            print('\tUsing vanilla CK3 de iure setup.')

    registerKeyword('ImperatorDeJure', getImperatorDeJure)


def readConfigurationFile(filename):
    registerKeyword('configuration', readConfigurationDetails)
    parseFile(filename)
    clearRegisteredKeywords()  # clears the configuration-related keywords, they are not used later

def getSavePath():
    if len(ImperatorSavePathOutput) == 0:
        return None
    return ImperatorSavePathOutput[0]
