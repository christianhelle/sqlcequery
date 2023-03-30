; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "SQL Compact Query Analyzer"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Christian Resma Helle"
#define MyAppURL "http://bit.ly/sqlcequery"   
#define MyAppExeName "QueryAnalyzer.exe"
#define MyAppIcon "Icon.ico"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{08CCB81F-78A8-4C63-8708-681140FF7C2A}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf32}\SQLCE Query Analyzer (x86)
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=.\Artifacts
OutputBaseFilename=SQLCEQueryAnalyzer-Setup-x86
SetupIconFile={#MyAppIcon}
Compression=lzma
SolidCompression=yes
ChangesAssociations=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: ".\Binaries\Release\x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: "Icon.ico"; DestDir: "{app}"; DestName: "Icon.ico"

[Icons]
Name: "{commondesktop}\SQL Compact Query Analyzer (x86)"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppIcon}"; Tasks: DesktopIcon

[Tasks]
Name: "DesktopIcon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Registry]
Root: HKLM; Subkey: "Software\Classes\.sdf"; ValueType: string; ValueName: ""; ValueData: "SQL Compact Database"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\Classes\SQL Compact Database"; ValueType: string; ValueName: ""; ValueData: "SQL Compact Database"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\SQL Compact Database\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKLM; Subkey: "Software\Classes\SQL Compact Database\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
