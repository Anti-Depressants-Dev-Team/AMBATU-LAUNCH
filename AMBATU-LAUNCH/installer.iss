[Setup]
AppName=AMBATU LAUNCH
AppVersion=1.0.2
AppPublisher=Antidepressants Dev Team
DefaultDirName={autopf}\AMBATU LAUNCH
DefaultGroupName=AMBATU LAUNCH
OutputBaseFilename=AMBATU_LAUNCH_Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=app.ico
UninstallDisplayIcon={app}\AMBATU-LAUNCH.exe

[Files]
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\AMBATU LAUNCH"; Filename: "{app}\AMBATU-LAUNCH.exe"
Name: "{autodesktop}\AMBATU LAUNCH"; Filename: "{app}\AMBATU-LAUNCH.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\AMBATU-LAUNCH.exe"; Description: "{cm:LaunchProgram,AMBATU LAUNCH}"; Flags: nowait postinstall skipifsilent
