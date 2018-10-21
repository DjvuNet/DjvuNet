#!/usr/bin/env bash

killall dotnet

__working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [ "$*" == "-all" ]
then
   echo "Cleaning entire repo with: git clean -xdf $__working_tree_root"
   git clean -xdf $__working_tree_root
   exit $?
fi

rm -rf obj
rm -rf build/bin
rm -rf packages
rm -f init-tools.log

exit $?
