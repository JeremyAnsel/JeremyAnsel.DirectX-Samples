@echo off
setlocal

cd "%~dp0"

@echo update tool
dotnet tool update dotnet-outdated-tool --tool-path packages

@echo update dependencies
packages\dotnet-outdated -r -u

