#ifndef MyAppVersion
  #define MyAppVersion "2.1.0"
#endif

#define MyAppName "WinCron"
#define MyAppPublisher "JackScott7"
#define MyAppExeName "wincron.exe"

[Setup]
AppId={{B2A30194-07E5-48D1-A267-D276C88673E9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\WinCron
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installer
OutputBaseFilename=wincron-setup
SetupIconFile=..\assets\wincron.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ChangesEnvironment=yes

[Dirs]
Name: "{commonappdata}\WinCron"
Name: "{commonappdata}\WinCron\output"

[Files]
Source: "..\artifacts\publish\win-x64\wincron.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "config.wc"; DestDir: "{commonappdata}\WinCron"; Flags: onlyifdoesntexist uninsneveruninstall

[Run]
Filename: "{sys}\sc.exe"; Parameters: "create WinCron start= auto obj= LocalSystem DisplayName= ""WinCron Scheduler"" binPath= """"{app}\{#MyAppExeName}"" --service --config ""{commonappdata}\WinCron\config.wc"""""; Flags: runhidden waituntilterminated; StatusMsg: "Installing WinCron service..."
Filename: "{sys}\sc.exe"; Parameters: "config WinCron start= auto obj= LocalSystem DisplayName= ""WinCron Scheduler"" binPath= """"{app}\{#MyAppExeName}"" --service --config ""{commonappdata}\WinCron\config.wc"""""; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "description WinCron ""Cron-style job scheduling for Windows"""; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "failure WinCron reset= 86400 actions= restart/5000/restart/15000/restart/60000"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "failureflag WinCron 1"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "start WinCron"; Flags: runhidden waituntilterminated; StatusMsg: "Starting WinCron service..."

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop WinCron"; Flags: runhidden waituntilterminated skipifdoesntexist; RunOnceId: "StopWinCronService"
Filename: "{sys}\sc.exe"; Parameters: "delete WinCron"; Flags: runhidden waituntilterminated skipifdoesntexist; RunOnceId: "DeleteWinCronService"

[Code]
function ReadMachinePath(): string;
var
  Paths: string;
begin
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Paths) then
  begin
    Result := '';
    exit;
  end;

  Result := Paths;
end;

procedure AddToMachinePath(Directory: string);
var
  Paths: string;
begin
  Paths := ReadMachinePath();
  if Pos(';' + Uppercase(Directory) + ';', ';' + Uppercase(Paths) + ';') = 0 then
  begin
    if (Paths <> '') and (Paths[Length(Paths)] <> ';') then
      Paths := Paths + ';';
    RegWriteExpandStringValue(HKEY_LOCAL_MACHINE,
      'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
      'Path', Paths + Directory);
  end;
end;

procedure RemoveFromMachinePath(Directory: string);
var
  Paths: string;
  PathEntry: string;
begin
  Paths := ';' + ReadMachinePath() + ';';
  PathEntry := ';' + Directory + ';';
  StringChangeEx(Paths, PathEntry, ';', True);
  while Pos(';;', Paths) > 0 do
    StringChangeEx(Paths, ';;', ';', True);
  if (Length(Paths) > 0) and (Paths[1] = ';') then
    Delete(Paths, 1, 1);
  if (Length(Paths) > 0) and (Paths[Length(Paths)] = ';') then
    Delete(Paths, Length(Paths), 1);
  RegWriteExpandStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', Paths);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\sc.exe'), 'stop WinCron', '', SW_HIDE,
    ewWaitUntilTerminated, ResultCode);
  Result := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    AddToMachinePath(ExpandConstant('{app}'));
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RemoveFromMachinePath(ExpandConstant('{app}'));
end;
