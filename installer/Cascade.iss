; Cascade for Windows - installer (Inno Setup 6)
#ifndef PublishDir
  #define PublishDir "..\publish"
#endif
#define AppVer "2.0.0"

[Setup]
AppId={{B7E2C1A4-6D5F-4C1E-9B3A-CA5CADE0A001}
AppName=Cascade
AppVersion={#AppVer}
AppPublisher=Falah Parkar
DefaultDirName={autopf}\Cascade
DefaultGroupName=Cascade
DisableProgramGroupPage=yes
OutputDir=..\installer-out
OutputBaseFilename=Cascade-Setup-{#AppVer}
SetupIconFile=..\Assets\icon.ico
UninstallDisplayIcon={app}\Cascade.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ChangesAssociations=yes

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\Cascade"; Filename: "{app}\Cascade.exe"
Name: "{autodesktop}\Cascade"; Filename: "{app}\Cascade.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Cascade.exe"; Description: "Launch Cascade"; Flags: nowait postinstall skipifsilent

[Registry]
; ---- "Open with" list ----
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""

Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".mp4"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".mkv"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".avi"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".mov"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".wmv"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".flv"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".webm"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".m4v"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".mpg"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".mpeg"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".ts"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".m2ts"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".3gp"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".ogv"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".vob"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".mts"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".divx"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".rmvb"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".mp3"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".m4a"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".aac"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".flac"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".wav"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".ogg"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".opus"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".wma"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".amr"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".mka"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".ape"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\Cascade.exe\SupportedTypes"; ValueType: string; ValueName: ".alac"; ValueData: ""

; ---- right-click menu on media files: "Play with Cascade" ----
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mp4\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mp4\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mp4\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mkv\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mkv\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mkv\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.avi\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.avi\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.avi\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mov\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mov\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mov\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wmv\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wmv\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wmv\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.flv\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.flv\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.flv\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.webm\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.webm\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.webm\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.m4v\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.m4v\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.m4v\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mpg\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mpg\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mpg\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mpeg\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mpeg\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mpeg\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ts\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ts\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ts\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.m2ts\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.m2ts\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.m2ts\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.3gp\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.3gp\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.3gp\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ogv\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ogv\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ogv\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.vob\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.vob\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.vob\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mts\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mts\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mts\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.divx\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.divx\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.divx\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.rmvb\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.rmvb\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.rmvb\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mp3\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mp3\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mp3\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.m4a\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.m4a\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.m4a\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.aac\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.aac\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.aac\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.flac\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.flac\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.flac\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wav\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wav\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wav\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ogg\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ogg\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ogg\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.opus\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.opus\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.opus\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wma\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wma\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.wma\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.amr\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.amr\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.amr\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mka\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mka\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.mka\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ape\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ape\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.ape\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.alac\shell\Cascade"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.alac\shell\Cascade"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\SystemFileAssociations\.alac\shell\Cascade\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" ""%1"""

; ---- right-click menu on folders ----
Root: HKA; Subkey: "Software\Classes\Directory\shell\CascadePlay"; ValueType: string; ValueName: ""; ValueData: "Play with Cascade"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Directory\shell\CascadePlay"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\Directory\shell\CascadePlay\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" --play-folder ""%1"""
Root: HKA; Subkey: "Software\Classes\Directory\shell\CascadeAdd"; ValueType: string; ValueName: ""; ValueData: "Add to Cascade library"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Directory\shell\CascadeAdd"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Cascade.exe"""
Root: HKA; Subkey: "Software\Classes\Directory\shell\CascadeAdd\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Cascade.exe"" --add-folder ""%1"""
