@echo off
setlocal

cd "%~dp0"

echo Build Dist

for /D %%a in ("*") do (
  if /I "%%a"=="DirectXRenderTargetScreenshot" (
    echo skip "%%a"
  ) else if /I "%%a"=="Images" (
    echo skip "%%a"
  ) else if /I "%%a"=="bld" (
    echo skip "%%a"
  ) else (
    echo process "%%a"
    for /D %%b in ("%%~a\*") do (
      if exist "%%~b\bin\Release\net5.0-windows\" (
        echo "%%~b"
        xcopy /s /d /q "%%~b\bin\Release\net5.0-windows" "bld\dist\%%~b\"
      )
    )
  )
)
