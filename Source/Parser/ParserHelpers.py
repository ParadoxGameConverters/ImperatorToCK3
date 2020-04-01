# the file is very WIP, but getStringList and getSingleString already work

from Parser.PyParser import peek_char, getNextLexeme, getNextTokenWithoutMatching, registerKeyword, parseStream, \
    registerRegex


def doNothing(unused, theStream):
    pass


def ignoreItem(unused, theStream):
    nextl = getNextLexeme(theStream)  # equals
    if nextl == '=':
        nextl = getNextLexeme(theStream)
    if nextl == '{':
        braceDepth = 1
        while True:
            if not peek_char(theStream):  # I use this instead of eof
                return
            token = getNextLexeme(theStream)
            if token == '{':
                braceDepth += 1
            elif token == '}':
                braceDepth -= 1
                if braceDepth == 0:
                    return


def getIntList(theStream):
    ints = []

    def intListFun1(theInt, theStream):
        ints.append(int(theInt))

    registerRegex(r'\d+', intListFun1)

    def intListFun2(theInt, theStream):
        newInt = theInt[1: len(theInt) - 1]
        ints.append(int(newInt))

    registerRegex(r'"\d+"', intListFun2)
    parseStream(theStream)

    return ints


def getSingleInt(theStream):
    getNextTokenWithoutMatching(theStream)  # gets equals sign
    token = getNextTokenWithoutMatching(theStream)
    if token[0] == "\"":
        token = token[1: len(token) - 1]
    try:
        theInt = int(token)
    except TypeError:
        print("Expected an int, but instead got " + token)
    return theInt


# possible additions:
# doubleList
# singleDouble


def getStringList(theStream):
    strings = []

    def appendString(theString, theStream):
        if theString[0] == '"':
            strings.append(theString[1: len(theString) - 1])
        else:
            strings.append(theString)

    registerKeyword(r'""', doNothing)
    registerRegex(r'(".*?")|([.\w]+)', appendString)
    parseStream(theStream)

    return strings


def getSingleString(theStream):
    theString = ''
    getNextTokenWithoutMatching(theStream)  # gets equals sign
    theString = getNextTokenWithoutMatching(theStream)
    if theString[0] == '"':
        theString = theString[1: len(theString) - 1]
    return theString
