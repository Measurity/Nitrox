#!/bin/bash

SCRIPTPATH="$( cd -- "$(dirname "$0")" >/dev/null 2>&1 ; pwd -P )"

RED=$(tput setaf 1)
BLUE=$(tput setaf 4)
BOLD=$(tput bold)
RESET=$(tput sgr0)

echo -e "► Navigating to '$SCRIPTPATH'\n"
cd "$SCRIPTPATH"

if [[ ! -x "$(command -v dotnet)" ]]; then
	echo -e "${RED}■ Unable to find 'dotnet' command, make sure you have everything installed and set up in PATH ${RESET}" >&2
	read -n 1 -r -t 2
	exit 1
fi

# Build
PROJECT_FOLDER_NAME="Nitrox.Launcher"
CSPROJECT_NAME="Nitrox.Launcher.csproj"
PUBLISH_DIRECTORY="$SCRIPTPATH/publish"

if [ -d "$PUBLISH_DIRECTORY" ]; then
	rm -rf "$PUBLISH_DIRECTORY"
fi

BUILD_START_TIME=`date +%s`

echo "Creating publish directory"
mkdir "$PUBLISH_DIRECTORY"

echo "Publishing MacOS App"
# UseAppHost => https://learn.microsoft.com/en-us/dotnet/core/install/macos-notarization-issues
dotnet publish "./$PROJECT_FOLDER_NAME/$CSPROJECT_NAME" --framework net8.0 --runtime osx-x64 --configuration Release -p:UseAppHost=true --output $PUBLISH_DIRECTORY
read -n 1 -r -t 2
echo "Done"

# Create .app bundle
APP_NAME="Nitrox"
INFO_PLIST="Info.plist"
INFO_PLIST_PATH="$PROJECT_FOLDER_NAME/$INFO_PLIST"
ICON_FILE="icon.icns"
ICON_FILE_PATH="$PROJECT_FOLDER_NAME/Assets/$ICON_FILE"

if [ ! -f "$INFO_PLIST_PATH" ]; then
	echo -e "${RED}■ Info.plist not found at '$INFO_PLIST_PATH' ${RESET}" >&2
    read -n 1 -r -t 2
	exit 1
fi

if [ ! -f "$ICON_FILE_PATH" ]; then
	echo -e "${RED}■ Icon file not found at '$ICON_FILE_PATH' ${RESET}" >&2
    read -n 1 -r -t 2
	exit 1
fi]

if [ -d "$APP_NAME" ]; then
	rm -rf "$APP_NAME"
fi

mkdir "$APP_NAME/Contents"
mkdir "$APP_NAME/Contents/MacOS"
mkdir "$APP_NAME/Contents/Resources"

cp "INFO_PLIST_PATH" "$APP_NAME/Contents/Info.plist"
cp "$ICON_FILE" "$APP_NAME/Contents/Resources/$ICON_FILE"
# cp -a "PUBLISH_DIRECTORY" "$APP_NAME/Contents/MacOS"

BUILD_END_TIME=`date +%s`

echo "► Executed in $(($BUILD_END_TIME - $BUILD_START_TIME))s"
read -n 1 -r -t 2