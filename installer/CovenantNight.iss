; Covenant Night — Windows installer (Inno Setup 6)
;
; 1. Build the player:  Unity -batchmode -projectPath Covenant_Night -executeMethod CovenantNightBuilder.BuildWindowsPlayerBatch
;    (or Covenant Night > Build Windows Player in the Editor). It writes Covenant_Night\Builds\Windows.
; 2. Compile this script:  ISCC.exe installer\CovenantNight.iss   ->  installer\Output\CovenantNight-Setup.exe
;    (the release workflow passes /DPlayerDir=<folder> and /DAppVersion=<x.y.z> to override the defaults below)

#define AppName      "Covenant Night"
#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#define AppPublisher "Covenant Night"
#define AppExe       "CovenantNight.exe"
#ifndef PlayerDir
  #define PlayerDir "..\Covenant_Night\Builds\Windows"
#endif

[Setup]
AppId={{6F2B8C1E-4D37-4A9B-9E15-C07E4A5D1B32}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
SetupIconFile=covenant-night.ico
WizardStyle=modern
WizardImageFile=wizard.bmp,wizard_150.bmp,wizard_200.bmp
WizardSmallImageFile=small.bmp,small_150.bmp,small_200.bmp
WizardImageBackColor=$200D06
OutputDir=Output
OutputBaseFilename=CovenantNight-Setup
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequiredOverridesAllowed=dialog
SetupLogging=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Night falls over Gibeah
WelcomeLabel2=Saul's spear is already in the air. Guide Jonathan as he leads David out through the sleeping city, past the guards and the torchlight, to the eastern gate.%n%nThis will install {#AppName} on your computer. It is best to close other applications before you continue.
SelectDirLabel3=Covenant Night will be installed in the following folder.
FinishedHeadingLabel=Go in peace
FinishedLabelNoIcons={#AppName} has been installed.%n%n"Go in peace, since we have sworn friendship with each other." (1 Samuel 20:42)
FinishedLabel={#AppName} has been installed. Start it from the shortcut you chose.%n%n"Go in peace, since we have sworn friendship with each other." (1 Samuel 20:42)

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#PlayerDir}\*"; DestDir: "{app}"; Excludes: "*_BurstDebugInformation_DoNotShip\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
