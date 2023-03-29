#!/bin/bash

export PROJECT_NAME='ApexSDK - Unity'
export PROJECT_VERSION=`cd ../; git tag | tail -n 1`
export PROJECT_BRIEF='Documentation for the Unity C# Library'
#export PROJECT_BRIEF='Documentation for the C# Library'
export PROJECT_STATUS='active'
export PROJECT_LOGO='docs-doxygen/doxygen-custom/defaultIcon.png'
export PROJECT_REPO='https://github.com/PixoVR/com.pixovr.apexunitysdk'
export PROJECT_URL='/ApexSDK-Unity'
export DEV_PROJECT_URL='../../../../Unity/com.pixovr.apexunitysdk/documentation/html/index.html'
export PROJECT_MAIN_PAGE='../pages/mainpage.md'

export DOXYGEN_FILTER='../scripts/unity_filter.py'
#export DOXYGEN_INPUT='"../pages"\n"../../Runtime"\n"../../Lib"\n"../../Samples"'
#export DOXYGEN_INPUT='"../pages"\n"../../Runtime"\n"../../Samples"'
export DOXYGEN_INPUT='../../Runtime'
export DOXYGEN_STRIP_FROM_PATH="$( cd "$( dirname "${BASH_SOURCE[0]}" )/../Runtime" && pwd)"
export DOXYGEN_IGNORE_PREFIX=''

export APEX_SERVER_URL='https://apex.pixovr.com'
