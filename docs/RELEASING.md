# Releasing WinCron

WinCron publishes one end-user installer, `wincron-setup.exe`, plus its SHA-256 checksum. The executable, runtime, initial service configuration, service registration, recovery policy, and uninstall support are contained in that installer.

## Repository setup

Configure these GitHub Actions secrets before creating a release tag:

- `WINDOWS_SIGNING_CERTIFICATE_BASE64`: the Base64-encoded Authenticode PFX file.
- `WINDOWS_SIGNING_CERTIFICATE_PASSWORD`: the PFX password.

Tagged releases fail when the signing certificate is missing. This prevents accidentally publishing an unsigned stable installer.

## Release process

1. Ensure `CHANGELOG.md`, `README.md`, and `ROADMAP.md` describe the shipped behavior.
2. Set `Version`, `AssemblyVersion`, and `FileVersion` in `WinCron.csproj`.
3. Merge the tested feature branch into `main`.
4. Create and push a matching tag, such as `v2.1.0` for project version `2.1.0`.
5. The release workflow restores, tests, publishes, signs, builds the installer, verifies its signature, calculates its checksum, and creates the GitHub Release.
6. The workflow records a successful `wincron-release` GitHub Deployment whose environment URL points to the Release page.

The workflow rejects a tag whose version does not match `WinCron.csproj`.

## Manual local build

```powershell
dotnet restore WinCron.sln
dotnet test WinCron.sln --configuration Release --no-restore
dotnet publish WinCron.csproj --configuration Release --runtime win-x64 --self-contained true --no-restore --output artifacts\publish\win-x64
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" /DMyAppVersion=2.1.0 installer\wincron.iss
```

The resulting installer is `artifacts\installer\wincron-setup.exe`. Local builds are unsigned unless they are signed separately with an Authenticode certificate.
