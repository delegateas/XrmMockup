@echo off
cls

.paket\paket.bootstrapper.exe
if errorlevel 1 (
    exit /b %errorlevel%
)

if exist paket.lock (
	.paket\paket.exe update
  if errorlevel 1 (
    exit /b %errorlevel%
  )
) else (
	.paket\paket.exe install
  if errorlevel 1 (
    exit /b %errorlevel%
  )
)


echo %*

PUSHD packages
FOR /R %%F IN (*.exe) DO (
	COPY /Y "%%F" "..\%%~nxF"
)
FOR /R %%F IN (*.dll) DO (
	COPY /Y "%%F" "..\%%~nxF"
)
FOR /R %%F IN (*.js) DO (
	COPY /Y "%%F" "..\%%~nxF"
)
POPD

RMDIR /S /Q packages
