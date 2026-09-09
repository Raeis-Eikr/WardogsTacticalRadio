# Wardogs Tactical Radio v0.1.0-alpha2a Packaging Fix

This update fixes tester-build version naming.

- `WardogsTacticalRadio.csproj` now carries the full prerelease version `0.1.0-alpha2a`.
- `Build-TesterPackage.ps1` reads the version from the project instead of hard-coding a package name.
- Future tester ZIP names will automatically match the version in the project file.

After applying, run:

```powershell
.\Build-TesterPackage.ps1
```

Expected package:

`WardogsRadio_v0.1.0-alpha2a_Windows_x64.zip`
