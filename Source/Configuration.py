from Parser.ParserHelpers import getSingleString
from Parser.PyParser import parseFile, registerKeyword
import os
import sys


def instantiate(unused, theStream):
    def verifyImperatorPath(path):
        if not os.path.isdir(path):
            sys.exit(path + ' does not exist!')
        if not os.path.isfile(path + r'\binaries\imperator.exe'):
            sys.exit(path + ' does not contain Imperator: Rome!')
        print('\tImperator: Rome install path is ' + path)

    ImperatorDirectoryOutput = []

    def getImperatorDirectory(unused, theStream):
        ImperatorDirectoryOutput.append('')
        ImperatorDirectoryOutput.append(getSingleString(theStream))
        verifyImperatorPath(ImperatorDirectoryOutput[1])

    registerKeyword('ImperatorDirectory', getImperatorDirectory)

    def verifyImperatorDocumentsPath(path):
        if not os.path.isdir(path):
            sys.exit(path + ' does not exist!')
        print('\tImperator: Rome documents directory path is ' + path)

    ImperatorDocumentsDirectoryOutput = []

    def getImperatorDocumentsDirectory(unused, theStream):
        ImperatorDocumentsDirectoryOutput.append('')
        ImperatorDocumentsDirectoryOutput.append(getSingleString(theStream))
        verifyImperatorDocumentsPath(ImperatorDocumentsDirectoryOutput[1])

    registerKeyword('ImperatorDocumentsDirectory', getImperatorDocumentsDirectory)

    # def verifyCK3Path(path):
    #     if not os.path.isdir(path):
    #         sys.exit(path + ' does not exist!')
    #     print('\tCK3 install path is ' + path)

    # CK3DirectoryOutput = []

    # def getCK3Directory(unused, theStream):
    #     CK3DirectoryOutput.append('')
    #    CK3DirectoryOutput.append(getSingleString(theStream))
    #     verifyCK3Path(CK3DirectoryOutput[1])

    # registerKeyword('CK3Directory', getCK3Directory)  # TODO #5: enable when CK3 is released

    # def verifyCK3DocumentsPath(path):
    #     if not os.path.isdir(path):
    #         sys.exit(path + ' does not exist!')
    #     print('\tCK3 documents directory path is ' + path)

    # CK3DocumentsDirectoryOutput = []

    # def getCK3DocumentsDirectory(unused, theStream):
    #     CK3DocumentsDirectoryOutput.append('')
    #     CK3DocumentsDirectoryOutput.append(getSingleString(theStream))
    #     verifyCK3DocumentsPath(CK3DocumentsDirectoryOutput[1])

    # registerKeyword('CK3DocumentsDirectory', getCK3DocumentsDirectory)  # TODO #5: enable when CK3 is released

    ImperatorDeJureOutput = []

    def getImperatorDeJure(unused, theStream):
        ImperatorDeJureOutput.append('')
        ImperatorDeJureOutput.append(getSingleString(theStream))
        if ImperatorDeJureOutput[1]=='yes':
            print('\tUsing Imperator: Rome provinces and regions to generate CK3 de iure.')
        else:
            print('\tUsing vanilla CK3 de iure setup.')
    registerKeyword('ImperatorDeJure', getImperatorDeJure)


def readConfigurationFile(filename):
    registerKeyword('configuration', instantiate)
    parseFile(filename)
