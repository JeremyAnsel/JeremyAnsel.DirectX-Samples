@echo off
setlocal

cd "%~dp0"

@echo update tool
dotnet tool update dotnet-outdated-tool --tool-path packages

@echo update dependencies
for /r %%f in (*.sln) do echo update dependencies in %%~nf && packages\dotnet-outdated -u "%%f"

