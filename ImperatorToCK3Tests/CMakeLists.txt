cmake_minimum_required (VERSION 3.13)
project(ImperatorToCK3Tests)

# add definitions
add_definitions(-fprofile-arcs -ftest-coverage)

# Include diretories
include_directories(/usr/include)
link_directories(/usr/lib)

# Set the compiler options
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++20 -g -O0 -Wall -fprofile-arcs -ftest-coverage")
set(CMAKE_CXX_OUTPUT_EXTENSION_REPLACE ON)

# Create OBJECT_DIR variable
set(OBJECT_DIR ${CMAKE_BINARY_DIR}/CMakeFiles/ImperatorToCK3Tests.dir)
message("-- Object files will be output to: ${OBJECT_DIR}")

# Find required packages
find_package(GTest REQUIRED)
include_directories(${GTEST_INCLUDE_DIRS})

# Set the sources
set(sources
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
				MapperTests/VersionParser/VersionParser.cpp
              
                #source files
                ../ImperatorToCK3/Source/Imperator/Characters/Character.cpp
                ../ImperatorToCK3/Source/Imperator/Characters/Character.cpp
                ../ImperatorToCK3/Source/Imperator/Characters/CharacterName.cpp
                ../ImperatorToCK3/Source/Imperator/Characters/CharacterAttributes.cpp
                ../ImperatorToCK3/Source/Imperator/Characters/Characters.cpp
                ../ImperatorToCK3/Source/Imperator/Families/Family.cpp
                ../ImperatorToCK3/Source/Imperator/Families/Families.cpp
				../ImperatorToCK3/Source/Imperator/Provinces/Province.cpp
				../ImperatorToCK3/Source/Imperator/Provinces/Provinces.cpp
				../ImperatorToCK3/Source/Imperator/Provinces/ProvinceName.cpp
				../ImperatorToCK3/Source/Imperator/Provinces/Pop.cpp
				../ImperatorToCK3/Source/Imperator/Provinces/Pops.cpp
                ../ImperatorToCK3/Source/Configuration/Configuration.cpp
                ../ImperatorToCK3/Source/Mappers/VersionParser/VersionParser.cpp
                ../commonItems/Parser.cpp
                ../commonItems/ParserHelpers.cpp
                ../commonItems/StringUtils.cpp
                ../commonItems/Date.cpp
                ../commonItems/Log.cpp
                ../commonItems/CommonFunctions.cpp
                ../commonItems/OSCommonLayer.cpp
                ../commonItems/iconvlite.cpp
                ../commonItems/LinuxUtils.cpp
                )
# Set Include directories
include_directories(
                ../ImperatorToCK3/
                ../commonItems/
                )

# Create the executable
add_executable(ImperatorToCK3Tests ${sources})

# Create the gcov target. Run coverage tests with 'make gcov'
add_custom_target(gcov
    COMMAND mkdir -p gcoverage
    COMMAND ${CMAKE_MAKE_PROGRAM} test
    WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
    )
add_custom_command(TARGET gcov
    COMMAND echo "=================== GCOV ===================="
    COMMAND gcov -b ${CMAKE_SOURCE_DIR}/src/*.cpp -o ${OBJECT_DIR}
    COMMAND echo "-- Source diretorie: ${CMAKE_SOURCE_DIR}/src/"
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
    COMMAND lcov --capture --directory ../ --output-file overage.info
    COMMAND echo "-- Generating HTML output files"
    COMMAND genhtml coverage.info --output-directory lcoverage
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
target_link_libraries(ImperatorToCK3Tests gtest)
target_link_libraries(ImperatorToCK3Tests gmock)
target_link_libraries(ImperatorToCK3Tests pthread)
target_link_libraries(ImperatorToCK3Tests -fprofile-arcs)
