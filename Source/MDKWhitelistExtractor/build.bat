@echo off
if not "%1"=="Release" goto skip
pushd %2
xcopy %3%4 ..\MDKBuild /y
popd
:skip