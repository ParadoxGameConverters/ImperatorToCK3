cmake_minimum_required (VERSION 3.13)
project(ImperatorToCK3Tests)
SET(CMAKE_CXX_COMPILER g++-11)

# add definitions
add_definitions(-fprofile-arcs -ftest-coverage)

# Link diretories
link_directories(../ImperatorToCK3/Resources /usr/local/lib)

# Set the compiler options
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++20 -g -O0 -Wall -fprofile-arcs --coverage")
set(CMAKE_CXX_OUTPUT_EXTENSION_REPLACE ON)

# Create OBJECT_DIR variable
set(OBJECT_DIR ${CMAKE_BINARY_DIR}/CMakeFiles/ImperatorToCK3Tests.dir)
message("-- Object files will be output to: ${OBJECT_DIR}")

# Find required packages
find_package(GTest REQUIRED)

# Set Include directories
include_directories(/usr/include)
include_directories(${GTEST_INCLUDE_DIRS})
include_directories(
	../ZipLib
	../json/single_include
	../cpp-base64
	/usr/local/include/ImageMagick-7/
	../ImperatorToCK3/Source
	../commonItems/
)

# Set the sources
set(test_sources
	test_main.cpp
	#the tests
	ImperatorWorldTests/Characters/CharacterTests.cpp
	ImperatorWorldTests/Characters/CharactersTests.cpp
	ImperatorWorldTests/Families/FamilyTests.cpp
	ImperatorWorldTests/Families/FamiliesTests.cpp
	ImperatorWorldTests/Provinces/ProvinceTests.cpp
	ImperatorWorldTests/Provinces/ProvincesTests.cpp
	ImperatorWorldTests/Provinces/PopTests.cpp
	ImperatorWorldTests/Provinces/PopsTests.cpp
	CommonTests/CommonFunctionsTests.cpp
)

file(GLOB CONFIGURATION_SOURCES "../ImperatorToCK3/Source/Configuration/*.cpp")
file(GLOB COMMON_UTILITIES_SOURCES "../ImperatorToCK3/Source/CommonUtilities/*.cpp")
file(GLOB IMPWORLD_SOURCES "../ImperatorToCK3/Source/Imperator/*.cpp")
file(GLOB IMPWORLD_SUBDIRS_SOURCES "../ImperatorToCK3/Source/Imperator/*/*.cpp")
file(GLOB MAPPERS_SOURCES "../ImperatorToCK3/Source/Mappers/*/*.cpp")
file(GLOB CK3WORLD_SOURCES "../ImperatorToCK3/Source/CK3/*.cpp")
file(GLOB CK3WORLD_SUBDIRS_SOURCES "../ImperatorToCK3/Source/CK3/*/*.cpp")
set(BASE64_SOURCES "../cpp-base64/base64.cpp")
set(COMMON_SOURCES "../commonItems/CommonFunctions.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/Color.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/ConvenientParser.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/ConverterVersion.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/GameVersion.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/Date.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/LinuxUtils.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/Log.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/OSCommonLayer.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/Parser.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/ParserHelpers.cpp")
set(COMMON_SOURCES ${COMMON_SOURCES} "../commonItems/StringUtils.cpp")

# Create the executable
add_executable(ImperatorToCK3Tests
	${test_sources}
	${CONFIGURATION_SOURCES}
	${COMMON_UTILITIES_SOURCES}
	${IMPWORLD_SOURCES}
	${IMPWORLD_SUBDIRS_SOURCES}
	${MAPPERS_SOURCES}
	${CK3WORLD_SOURCES}
	${CK3WORLD_SUBDIRS_SOURCES}
	${COMMON_SOURCES}
	${BASE64_SOURCES}
)

# Create the gcov target. Run coverage tests with 'make gcov'
add_custom_target(gcov
    COMMAND mkdir -p gcoverage
    COMMAND ${CMAKE_MAKE_PROGRAM} test
    WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
)
add_custom_command(TARGET gcov
    COMMAND echo "=================== GCOV ===================="
    COMMAND gcov -b ${CMAKE_SOURCE_DIR}/*.cpp -o ${OBJECT_DIR}
    COMMAND gcov -v
    COMMAND echo "-- Source directory: ${CMAKE_SOURCE_DIR}/"
    COMMAND echo "-- Coverage files have been output to ${CMAKE_BINARY_DIR}/gcoverage"
    WORKING_DIRECTORY ${CMAKE_BINARY_DIR}/gcoverage
)
add_dependencies(gcov ImperatorToCK3Tests)

# Create the lcov target. Run result tests with 'make lcov'
add_custom_target(lcov
    COMMAND mkdir -p lcoverage
)
add_custom_command(TARGET lcov
    COMMAND echo "=================== LCOV ===================="
    COMMAND echo "-- Passing lcov tool under code coverage"
    COMMAND lcov --capture --directory ../ --output-file lcoverage/coverage.info
    COMMAND echo "-- Generating HTML output files"
    COMMAND genhtml lcoverage/coverage.info --output-directory lcoverage
)

# Make sure to clean up the coverage folder
set_property(DIRECTORY APPEND PROPERTY ADDITIONAL_MAKE_CLEAN_FILES gcoverage)

# Create the gcov-clean target. This cleans the build as well as generated 
# .gcda and .gcno files.
add_custom_target(init
    COMMAND ${CMAKE_MAKE_PROGRAM} clean
    COMMAND rm -f ${OBJECT_DIR}/*.gcno
    COMMAND rm -f ${OBJECT_DIR}/*.gcda
    WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
)

# Testing
enable_testing()

add_test(output_test ${CMAKE_CURRENT_BINARY_DIR}/ImperatorToCK3Tests)

# link libraries to the new target
target_link_libraries(ImperatorToCK3Tests LINK_PUBLIC
	gtest
	pthread
	rakaly
	-fprofile-arcs
)
