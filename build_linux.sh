#!/bin/sh
# shellcheck disable=SC2103

git submodule update --init --recursive &&
sudo apt-get install libcurl4-openssl-dev &&
sudo apt-key adv --fetch-keys https://repos.codelite.org/CodeLite.asc &&
sudo apt-add-repository 'deb https://repos.codelite.org/wx3.1.5/ubuntu/ focal universe' &&
sudo add-apt-repository ppa:ubuntu-toolchain-r/test &&
sudo apt-get update &&
sudo apt-get install gcc-11 g++-11 &&
sudo apt-get install libwxbase3.1-0-unofficial libwxbase3.1unofficial-dev libwxgtk3.1-0-unofficial libwxgtk3.1unofficial-dev wx3.1-headers wx-common &&
# Link gcc-11 and g++-11 to their standard commands
sudo ln -s /usr/bin/gcc-11 /usr/local/bin/gcc &&
sudo ln -s /usr/bin/g++-11 /usr/local/bin/g++ &&
# Export CC and CXX to tell cmake which compiler to use
export CC=/usr/bin/gcc-11 &&
export CXX=/usr/bin/g++-11 &&
# Check versions of gcc, g++ and cmake
gcc -v && g++ -v && cmake --version &&

rm -rf Release/ &&
cd Fronter &&
./build_linux.sh &&
mv Release ../ &&
cd ../ImperatorToCK3 &&
dotnet restore &&
dotnet build &&
cd .. &&

tar -cjf ImperatorToCK3-Linux.tar.bz2 Release
