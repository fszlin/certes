
rmdir /S /Q "./build"

dotnet restore ./src/Certes/project.json
dotnet pack -c release -o ./build/nuget ./src/Certes/project.json

dotnet restore ./src/Certes.Cli/project.json
dotnet publish -f netcoreapp1.0 -c release -o ./build/cli/bin ./src/Certes.Cli/project.json

@echo off
echo dotnet ./bin/Certes.Cli.dll %%^* > ./build/cli/certes.bat
@echo on
