# NWNBuilder

Desktop-program til at lave og gemme Neverwinter Nights character builds. Erstatter build-regneark og laver beregningerne. Arelith er første regelsæt.

- C# / .NET 10, Avalonia (Windows og Linux)
- Builds gemmes i MongoDB (online)

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

### Feats

Hvert feat har beskrivelse, krav (level, BAB, stats, andre feats, skills, klasse-levels) og evt. Arelith-ændringer.
Kravene tjekkes pr. level i **Levels**-fanen (kolonnen *Krav* og panelet under tabellen). Epic feats kan kun vælges fra level 21.
Feats som en klasse eller race giver automatisk (`grantedFeats`) tæller som taget, og deres krav tjekkes ikke.
Skills gemmes ikke pr. level, så skill-krav tjekkes mod det højst mulige antal ranks på det level.
Spell level-krav (fx "kan kaste 9. cirkel") vises kun som tekst.

Kilder: vanilla-krav og -beskrivelser er fra [NWN Wiki](https://nwn.fandom.com/) (CC BY-SA 3.0),
hentet via [nwn-siala-builder](https://github.com/Danichyo/nwn-siala-builder); Arelith-ændringer fra
[wiki.nwnarelith.com/Feat_changes](https://wiki.nwnarelith.com/Feat_changes). Feat-delen af `rulesets/arelith.json`
er derfor også CC BY-SA 3.0.
