# CommandWizard v1.0.0

CommandWizard v1.0.0 is the first local signed MSIX release of the schema-driven command builder. It packages the desktop app as **Aptlantis Command Wizard** with bundled Windows App SDK runtime files, local signing, and the final Command Wizard logo assets.

## Highlights

- Builds CLI commands from TOML tool schemas.
- Supports schema editing, help-text import, options, parameters, tags, and favorite commands.
- Ships with a local signed MSIX package identity version of `1.0.0.0`.
- Uses the final Command Wizard logo for Store, square, and wide tile assets.
- Includes a repeatable packaging script at `scripts\package-msix.ps1`.

## Installer

The release package is generated at:

```text
dist\CommandWizard-1.0.0.0.msix
```

The package is signed with the local development certificate:

```text
Subject: CN=Administrator
Thumbprint: C03F83DE2A4964918A6D1D10C0F5D88B39647B11
Expires: 2027-03-14
```

This certificate is intended for local machines and trusted testers only.

## Install

Trust the certificate once on each machine:

```powershell
winapp cert install .\devcert.pfx
```

Install the MSIX:

```powershell
Add-AppxPackage .\dist\CommandWizard-1.0.0.0.msix
```

Launch **Aptlantis Command Wizard** from the Start menu.

## Verification

The v1.0.0 release was verified with:

```powershell
dotnet build .\CommandWizard.csproj -c Release
dotnet test .\CommandWizard.Tests\CommandWizard.Tests.csproj -c Release
.\scripts\package-msix.ps1
Get-AuthenticodeSignature .\dist\CommandWizard-1.0.0.0.msix
Add-AppxPackage .\dist\CommandWizard-1.0.0.0.msix
```

Results:

- Release build succeeded.
- Test suite passed: `11/11`.
- MSIX signature status: `Valid`.
- Installed package version: `1.0.0.0`.
- Installed app launched with title `Aptlantis Command Wizard`.

## Notes

- This is a local signed release, not a Microsoft Store submission.
- Public distribution should replace `devcert.pfx` with a trusted code-signing certificate and update the manifest publisher to match.
