@echo off
:: Add the paths for the F# SDK 3.x (from higher version to lower)
set FSHARPSDK=^
C:\Program Files (x86)\Microsoft SDKs\F#\4.0\Framework\v4.0\;^
C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\;^
C:\Program Files (x86)\Microsoft SDKs\F#\3.0\Framework\v4.0\

set CSHARPSDK=^
C:\Program Files (x86)\MSBuild\14.0\Bin\


cls
for %%i in (fsianycpu.exe) do (
	if exist "%%~$FSHARPSDK:i" set HASFSHARP=1
	)
for %%i in (csi.exe) do (
	if exist "%%~$CSHARPSDK:i" set HASCSHARP=1
	)

if defined HASFSHARP (
	:: Execute the script "only" with the first "fsianycpu.exe" found
	for %%i in (fsianycpu.exe) do "%%~$FSHARPSDK:i" MetadataGen\Metadata.fsx
) else (
	echo fsi wasn't found
	pause
	::if defined HASCSHARP (
	::	:: Execute the script "only" with the first "csi.exe" found
	::	for %%i in (csi.exe) do (
	::		"%%~$CSHARPSDK:i" MetadataGen\GenTemp.csx
	::		"%%~$CSHARPSDK:i" MetadataGen\Metadata.csx
	::		)
	::	pause
	::) else (
	::	echo Neither fsi  or csi was found
	::	pause
	::)
) 