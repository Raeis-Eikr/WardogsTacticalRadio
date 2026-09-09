# WARDOGS Tactical Radio — v0.1.0-alpha1 Foundation

This is the first foundation build for the locked WARDOGS tactical-radio design.

## What alpha1 contains
- Windows WPF desktop shell using .NET 10.
- Locked WR-25 rugged tactical-radio visual direction.
- Same executable can **Host** or **Join** a radio net.
- Basic LAN TCP session creation and state synchronization.
- Persistent session identity and peer list model.
- Squad (`SQD`) and Command (`CMD`) channel definitions in the core session model.
- Persistent local storage framework for Friends, Known Servers, and Recent Sessions.
- Host eligibility and deterministic replacement-host election foundation.
- No voice/audio transport yet.
- Automatic host migration is **architecturally prepared but not active in alpha1**.

## Locked design scope
No new features should be added unless required to make the existing locked systems function:
1. Every copy is both host/client capable.
2. Automatic host migration/failover + manual host transfer.
3. Squad and Command voice channels.
4. Simultaneous monitoring.
5. Separate PTT controls.
6. Role/channel permissions.
7. Distributed session state.
8. WR-25 rugged military-radio UI.
9. Friends list.
10. Known/saved servers.
11. Recent sessions.
12. Favorites.
13. Join/invite actions.
14. Persistent network/session IDs.

## First run
Open `WardogsRadio.sln` in VS Code, then in the integrated terminal run:

```powershell
dotnet restore
dotnet build
dotnet run --project .\WardogsTacticalRadio\WardogsTacticalRadio.csproj
```

## LAN test
On PC 1, click **HOST RADIO NET**. The local address shown in the left rail is the address PC 2 should use.

On PC 2, enter PC 1's IPv4 address in **JOIN RADIO NET**, then click **JOIN RADIO NET**.

Both PCs must currently use TCP port `47825`. Windows Firewall may prompt you the first time; allow the app on **Private networks** for LAN testing.

## Runtime data
Settings are stored in:

`%APPDATA%\WardogsTacticalRadio\settings.json`

Do not delete that file between tests unless we intentionally want a clean profile.
