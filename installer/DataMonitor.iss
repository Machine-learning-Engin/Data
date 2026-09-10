#define MyAppName "DataMonitor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "DataMonitor"
#define MyAppExeName "DataMonitor.App.exe"

[Setup]
AppId={{A8B0B9D5-8D6D-4E73-9D75-4D7A7B4D3E21}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}

DefaultDirName={autopf}\DataMonitor
DefaultGroupName=DataMonitor

OutputDir=..\artifacts\installer
OutputBaseFilename=DataMonitor-{#MyAppVersion}-Setup

Compression=lzma
SolidCompression=yes

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

PrivilegesRequired=admin

SetupIconFile=..\src\DataMonitor.App\Assets\DataMonitor.ico
UninstallDisplayIcon={app}\DataMonitor.App.exe

DisableProgramGroupPage=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; \
    Description: "Create a desktop shortcut"; \
    GroupDescription: "Additional shortcuts:"; \
    Flags: unchecked

[Files]
Source: "..\artifacts\release\win-x64\*"; \
    DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\DataMonitor"; \
    Filename: "{app}\DataMonitor.App.exe"; \
    WorkingDir: "{app}"

Name: "{group}\Uninstall DataMonitor"; \
    Filename: "{uninstallexe}"

Name: "{autodesktop}\DataMonitor"; \
    Filename: "{app}\DataMonitor.App.exe"; \
    WorkingDir: "{app}"; \
    Tasks: desktopicon

[Run]
Filename: "{app}\DataMonitor.App.exe"; \
    Description: "Launch DataMonitor"; \
    Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"