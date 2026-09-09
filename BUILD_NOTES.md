# WARDOGS Tactical Radio v0.1.0-alpha2b — Packaging Correction

This correction fixes release metadata for the alpha2b tester build and hardens the packaging process against future version mismatches.

## Corrected
- `WardogsTacticalRadio.csproj` now reports `0.1.0-alpha2b`.
- Tester package output will now be named `WardogsRadio_v0.1.0-alpha2b_Windows_x64.zip`.
- Packaging now validates that the project version, visible UI version in `MainWindow.xaml.cs`, and `BUILD_NOTES.md` all agree before publishing.
- The packaging script stops with an error if those release identifiers do not match.

## Expected packaging output
- UI: `v0.1.0-alpha2b`
- Project version: `0.1.0-alpha2b`
- Packaging message: `Building tester package for v0.1.0-alpha2b...`
- ZIP: `WardogsRadio_v0.1.0-alpha2b_Windows_x64.zip`

No networking or feature behavior is changed by this correction.
