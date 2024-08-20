#!/bin/bash

# Make script agnostic of where it is called from
SCRIPTPATH="$( cd -- "$(dirname "$0")" >/dev/null 2>&1 ; pwd -P )"
cd "$SCRIPTPATH"

# Remove everything, since dotnet clean is still unable to properly clean
rm -rf ./*/bin ./*/obj

# Build

PROJECT_FOLDER_NAME="Nitrox.Launcher"
CSPROJECT_NAME="Nitrox.Launcher.csproj"
PUBLISH_DIRECTORY="$SCRIPTPATH/publish"

mkdir "$PUBLISH_DIRECTORY"

# UseAppHost => https://learn.microsoft.com/en-us/dotnet/core/install/macos-notarization-issues
dotnet publish "./$PROJECT_FOLDER_NAME/$CSPROJECT_NAME" --framework net8.0 --runtime osx-x64 --configuration Release -p:UseAppHost=true --output $PUBLISH_DIRECTORY

# Create .app bundle

APP_NAME="Nitrox"
INFO_PLIST="Info.plist"
INFO_PLIST_PATH="$PROJECT_FOLDER_NAME/$INFO_PLIST"
ICON_FILE="icon.icns"
ICON_FILE_PATH="$PROJECT_FOLDER_NAME/Assets/$ICON_FILE"

if [ ! -f "$INFO_PLIST_PATH" ]; then
    echo "Error: Info.plist not found at $INFO_PLIST_PATH"
    exit 1
fi

if [ ! -f "$ICON_FILE_PATH" ]; then
    echo "Error: Icon file not found at $ICON_FILE_PATH"
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

# Make sure permissions are OK
# ZIP everything into .app file