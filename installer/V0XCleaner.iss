; Script Inno Setup 6 — V0X Cleaner. Compilé par installer\build.ps1 (définit AppVersion, PublishDir, OutputDir).
#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif

[Setup]
AppId={{B7C1E5D2-4A0F-4D8B-9C63-5E2F0A7D1B0C}
AppName=V0X Cleaner
AppVersion={#AppVersion}
AppPublisher=V0X
DefaultDirName={autopf}\V0X Cleaner
DefaultGroupName=V0X Cleaner
UninstallDisplayIcon={app}\V0XCleaner.exe
UninstallDisplayName=V0X Cleaner
OutputDir={#OutputDir}
OutputBaseFilename=V0XCleaner-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
WizardStyle=modern
SetupIconFile=..\src\V0XCleaner.App\Resources\app.ico

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\V0X Cleaner"; Filename: "{app}\V0XCleaner.exe"
Name: "{group}\Désinstaller V0X Cleaner"; Filename: "{uninstallexe}"
Name: "{autodesktop}\V0X Cleaner"; Filename: "{app}\V0XCleaner.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\V0XCleaner.exe"; Description: "Lancer V0X Cleaner"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""V0XCleaner AutoClean"" /F"; Flags: runhidden; RunOnceId: "DelAutoCleanTask"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "V0XCleaner"; Flags: uninsdeletevalue dontcreatekey
