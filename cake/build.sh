#!/usr/bin/env bash
###############################################################
# This is the Cake bootstrapper script that is responsible for
# downloading Cake and all specified tools from NuGet.
###############################################################

# Define directories.
SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
TOOLS_DIR=$SCRIPT_DIR/tools
NUGET_EXE=$TOOLS_DIR/nuget.exe
CAKE_EXE=$TOOLS_DIR/Cake/Cake.exe
MONO_CMD=mono

# Define default arguments.
SCRIPT="build.cake"
TARGET="default"
CONFIGURATION="Release"
VERBOSITY="diagnostic"
DRYRUN=
SHOW_VERSION=false
SCRIPT_ARGUMENTS=()

# Run without mono on windows platforms.
case "$(uname -s)" in
    CYGWIN*) unset MONO_CMD ;;
    MINGW32*) unset MONO_CMD ;;
	MINGW64*) unset MONO_CMD ;;
    MSYS*) unset MONO_CMD ;;
esac

# Parse arguments.
while [[ $# -gt 0 ]]; do
    case $1 in
        -s|--script) SCRIPT="$2"; shift ;;
        -t|--target) TARGET="$2"; shift ;;
        -c|--configuration) CONFIGURATION="$2"; shift ;;
        -v|--verbosity) VERBOSITY="$2"; shift ;;
        -d|--dryrun) DRYRUN="-dryrun" ;;
        --version) SHOW_VERSION=true ;;
        --) shift; SCRIPT_ARGUMENTS+=("$@"); break ;;
        *) SCRIPT_ARGUMENTS+=("$1") ;;
    esac
    shift
done

# Make sure the tools folder exist.
if [ ! -d $TOOLS_DIR ]; then
  mkdir $TOOLS_DIR
fi

# Make sure that packages.config exist.
if [ ! -f $TOOLS_DIR/packages.config ]; then
    echo "Downloading packages.config..."
    curl -Lsfo $TOOLS_DIR/packages.config http://cakebuild.net/download/bootstrapper/packages
    if [ $? -ne 0 ]; then
        echo "An error occured while downloading packages.config."
        exit 1
    fi
fi

# Download NuGet if it does not exist.
if [ ! -f $NUGET_EXE ]; then
    echo "Downloading NuGet..."
    curl -Lsfo $NUGET_EXE https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
    if [ $? -ne 0 ]; then
        echo "An error occured while downloading nuget.exe."
        exit 1
    fi
fi

# Restore tools from NuGet.
pushd $TOOLS_DIR >/dev/null
$MONO_CMD $NUGET_EXE install -ExcludeVersion
if [ $? -ne 0 ]; then
    echo "Could not restore NuGet packages."
    exit 1
fi
popd >/dev/null

# Make sure that Cake has been installed.
if [ ! -f $CAKE_EXE ]; then
    echo "Could not find Cake.exe at '$CAKE_EXE'."
    exit 1
fi

# Start Cake
if $SHOW_VERSION; then
    exec $MONO_CMD $CAKE_EXE -version
else
    echo "Script arguments: $SCRIPT_ARGUMENTS"
    exec $MONO_CMD $CAKE_EXE $SCRIPT -verbosity=$VERBOSITY -configuration=$CONFIGURATION -target=$TARGET $DRYRUN --settings_skipverification=true "${SCRIPT_ARGUMENTS[@]}"
fi