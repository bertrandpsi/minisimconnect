# A minimalistic example of a SimConnect software

To create your own SimProject make sure of:

1. Install the FS2020 SDK (we assume you have it installed in "C:\MSFS SDK")
2. Your project must be a 64 bit assembly
3. You must have a post build event which copies the DLL and config:
```
xcopy "C:\MSFS SDK\SimConnect SDK\lib\SimConnect.dll" "$(TargetDir)" /y
xcopy "C:\MSFS SDK\Samples\SimvarWatcher\SimConnect.cfg" "$(TargetDir)" /y
```

Have fun!