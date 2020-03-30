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

    ImperatordirectoryOutput = []

    def getImperatordirectory(unused, theStream):
        ImperatordirectoryOutput.append('')  # ImperatordirectoryOutput[0] empty, keyword is not used
        ImperatordirectoryOutput.append(getSingleString(theStream))
        verifyImperatorPath(ImperatordirectoryOutput[1])

    registerKeyword('Imperatordirectory', getImperatordirectory)

    def verifyImperatorDocumentsPath(path):
        if not os.path.isdir(path):
            sys.exit(path + ' does not exist!')
        print('\tImperator: Rome documents directory path is ' + path)

    ImperatorDocumentsDirectoryOutput = []

    def getImperatorDocumentsDirectory(unused, theStream):
        ImperatorDocumentsDirectoryOutput.append('')  # ImperatorDocumentsDirectoryOutput[0] empty, keyword is not used
        ImperatorDocumentsDirectoryOutput.append(getSingleString(theStream))
        verifyImperatorDocumentsPath(ImperatorDocumentsDirectoryOutput[1])

    registerKeyword('ImperatorDocumentsDirectory', getImperatorDocumentsDirectory)

    def verifyCK3Path(path):
        if not os.path.isdir(path):
            sys.exit(path + ' does not exist!')
        print('\tCK3 install path is ' + path)

    CK3directoryOutput = []

    def getCK3directory(unused, theStream):
        CK3directoryOutput.append('')  # CK3directoryOutput[0] empty, keyword is not used
        CK3directoryOutput.append(getSingleString(theStream))
        verifyCK3Path(CK3directoryOutput[1])

    # registerKeyword('CK3directory', getCK3directory)  # TODO: enable when CK3 is released

    def verifyCK3DocumentsPath(path):
        if not os.path.isdir(path):
            sys.exit(path + ' does not exist!')
        print('\tCK3 documents directory path is ' + path)

    CK3DocumentsdirectoryOutput = []

    def getCK3Documentsdirectory(unused, theStream):
        CK3DocumentsdirectoryOutput.append('')  # CK3DocumentsdirectoryOutput[0] empty, keyword is not used
        CK3DocumentsdirectoryOutput.append(getSingleString(theStream))
        verifyCK3DocumentsPath(CK3DocumentsdirectoryOutput[1])

    # registerKeyword('CK3Documentsdirectory', getCK3Documentsdirectory)  # TODO: enable when CK3 is released

    Imperator_de_iureOutput = []

    def getImperator_de_iure(unused, theStream):
        Imperator_de_iureOutput.append('')  # Imperator_de_iureOutput[0] empty, keyword is not used
        Imperator_de_iureOutput.append(getSingleString(theStream))
        if Imperator_de_iureOutput[1]=='yes':
            print('\tUsing Imperator: Rome provinces and regions to generate CK3 de iure.')
        else:
            print('\tUsing vanilla CK3 de iure setup.')
    registerKeyword('Imperator_de_iure', getImperator_de_iure)


def readConfigurationFile(filename):
    registerKeyword('configuration', instantiate)
    parseFile(filename)
