#define MyAppName "Family Management"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "Frederico Ferreira"
#define MyAppExeName "Family_Management.WPF.exe"
#define PublishSource "C:\Users\frede\source\repos\Family_Management.WPF\Family_Management.WPF\bin\Publish\WindowsLocal"

[Setup]
; Manter este identificador nas futuras versões.
AppId={{EA83C25D-1E5D-4773-928B-2CAB0F3FE017}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

; Instalar os ficheiros do programa separados dos dados.
DefaultDirName={localappdata}\Programs\FamilyManagement
DefaultGroupName={#MyAppName}

PrivilegesRequired=lowest

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

DisableProgramGroupPage=yes
DisableDirPage=yes
UsePreviousAppDir=yes

OutputDir=Output
OutputBaseFilename=FamilyManagement-Setup-{#MyAppVersion}

Compression=lzma2
SolidCompression=yes
WizardStyle=modern

UninstallDisplayIcon={app}\{#MyAppExeName}

CloseApplications=yes
CloseApplicationsFilter=*.exe,*.dll
RestartApplications=no

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar um atalho no ambiente de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
Source: "{#PublishSource}\*"; DestDir: "{app}"; Excludes: "*.db,*.db-*,*.sqlite,*.sqlite3,*.pdb,*.cs,*.log,*.key,Security\*,Backups\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent