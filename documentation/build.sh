#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd $DIR/doxygen

source ../env.sh

rm -rf ../html
cp -rfv ../../*md ../pages

doxygen doxyfile

cp -rv images ../html/images

cd ../

