; Inno Setup script — compile on Windows after publishing the app.
; Requires Inno Setup 6: https://jrsoftware.org/isinfo.php

#ifndef PublishDir
  #define PublishDir "..\dist\publish"
#endif

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName "PingIt"
#define MyAppPublisher "PingIt"
#define MyAppExeName "PingIt.exe"
#define MyAppURL "https://github.com/Muds1r/PingIt"

[Setup]
AppId={{8F4E2A91-3C5D-4B7E-9F12-0A1B2C3D4E5F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist\installer
OutputBaseFilename=PingIt-Setup-{#MyAppVersion}
SetupIconFile=compiler:SetupClassicIcon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "launch"; Description: "Launch {#MyAppName} after installation"; GroupDescription: "Other tasks:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Tasks: launch

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
