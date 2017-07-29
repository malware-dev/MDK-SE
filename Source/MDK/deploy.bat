@echo off
if not "%1" == "Release" goto end

xcopy %3 %2 /Y

:end