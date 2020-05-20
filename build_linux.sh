#!/bin/sh

export CC=/usr/bin/gcc-9
export CXX=/usr/bin/g++-9

rm -rf Release/
cd Fronter
./build_linux.sh
mv Release ../
cd ../ImperatorToCK3 || exit
rm -rf build
rm -rf Release-Linux
cmake -H. -Bbuild
cmake --build build -- -j3 
mv Release-Linux ../Release/ImperatorToCK3
cd ..

cp ImperatorToCK3/Data_Files/*yml Release/Configuration/
cp ImperatorToCK3/Data_Files/fronter*txt Release/Configuration/

tar -cjf ImperatorToCK3-dev-release.tar.bz2 Release
