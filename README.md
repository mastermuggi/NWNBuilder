# NWNBuilder

Desktop-program til at lave og gemme Neverwinter Nights character builds. Erstatter build-regneark og laver beregningerne. Arelith er første regelsæt.

- C# / .NET 10, Avalonia (Windows og Linux)
- Builds gemmes i MongoDB (online) – ikke tilsluttet endnu

## Struktur

- `src/NWNBuilder` – Avalonia-appen
- `src/NWNBuilder.Core` – datamodel (build, levels) og eksempel-builds
- `tests/NWNBuilder.Tests` – tests

## Hent programmet

GitHub → Actions → seneste kørsel på `main` → artefakt `NWNBuilder-windows` (eller `NWNBuilder-linux`). Pak ud og kør `NWNBuilder.exe`.
