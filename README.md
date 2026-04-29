# CommandWizard

CommandWizard is a Windows desktop app for building CLI commands from editable TOML tool schemas. It loads schemas, lets you choose actions, flags, options, and parameters, then produces a copyable command preview and saved favorites.

## Release Status

Current target: `v1.0.0` local signed MSIX.

- App package version: `1.0.0.0`
- Package identity publisher: `CN=Administrator`
- Signing certificate: `devcert.pfx` local development certificate
- Output package: `dist\CommandWizard-1.0.0.0.msix`

The development certificate is intended for local machines and trusted testers only. For public distribution, replace it with a trusted code-signing certificate and update the manifest publisher to match.

## Build And Test

```powershell
dotnet build .\CommandWizard.csproj -c Release
dotnet test .\CommandWizard.Tests\CommandWizard.Tests.csproj -c Release
winapp cert info .\devcert.pfx
```

The certificate subject must match the manifest publisher:

```text
Subject: CN=Administrator
```

## Package MSIX

Use the release script:

```powershell
.\scripts\package-msix.ps1
```

The script publishes a self-contained win-x64 Release build with the Windows App SDK runtime bundled into the publish folder:

```text
dist\v1.0.0\win-x64\publish
```

Then it creates and signs:

```text
dist\CommandWizard-1.0.0.0.msix
```

Equivalent manual commands:

```powershell
dotnet publish .\CommandWizard.csproj -c Release -r win-x64 --self-contained true /p:WindowsAppSDKSelfContained=true -o .\dist\v1.0.0\win-x64\publish
winapp package .\dist\v1.0.0\win-x64\publish --manifest .\appxmanifest.xml --cert .\devcert.pfx --cert-password password --exe CommandWizard.exe --output .\dist\CommandWizard-1.0.0.0.msix
```

## Install Local Signed Build

Trust the development certificate once on each machine:

```powershell
winapp cert install .\devcert.pfx
```

Then install:

```powershell
Add-AppxPackage .\dist\CommandWizard-1.0.0.0.msix
```

Launch **Aptlantis Command Wizard** from the Start menu.

## Smoke Test

After installing the MSIX:

- Confirm the app launches as **Aptlantis Command Wizard**.
- Confirm the bundled `rsync` schema loads.
- Toggle an option and verify the command preview updates.
- Copy or save a command and confirm the app remains responsive.
- Confirm the package logo/display name appears correctly in Windows.
