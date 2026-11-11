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

## Regelsæt

Reglerne ligger som JSON i `rulesets/` (fx `rulesets/arelith.json`) og følger med i zip-filen fra Actions.
I programmet: **Forbind** → **Importér regelsæt…** → vælg filen. Den gemmes i MongoDB (collection `rulesets`),
og et build bruger regelsættet i feltet *Regelsæt*. Ret JSON-filen og importér igen for at opdatere.

Indhold: klasser (hit die, BAB, saves, skillpoints, class skills, save-bonusser), races, gifts, skills og feats
med effekt på stats/saves. Felter markeret *unverified* i `notes` skal tjekkes.
