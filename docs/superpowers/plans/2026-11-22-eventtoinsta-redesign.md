# EventToInsta Redesign – Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wochenvorschau (montags) und Tagesergebnisse (stündlich 12–23 Uhr) des SV Bad Laer werden automatisch als Instagram-Stories im neuen Design gepostet, und zwar ohne Doppelposts.

**Architecture:** Der Ablauf ist in Schichten getrennt: Datenquelle (basketball-bund.net) → Fachmodell `Game` → reine Planer (was wird gepostet) → SkiaSharp-Vorlagen (wie sieht es aus) → Publisher (upload-post.com oder lokaler Ordner) und Zustand (`posted.json` in GCS oder als Datei). Zwei Jobs (`PreviewJob`, `ResultsJob`) verbinden das. Sie werden über die Konsole oder die Cloud Function (`?job=`) gestartet. Die Abhängigkeiten werden von Hand in einem `CompositionRoot` zusammengesetzt.

**Tech Stack:** .NET 8, SkiaSharp 3.x, Topten.RichTextKit, Flurl.Http, Google.Cloud.Functions.Framework, Google.Cloud.Storage.V1, xUnit

**Spec:** `docs/superpowers/specs/2026-11-22-eventtoinsta-redesign-design.md`. Die Spec ist verbindlich. Dieser Plan legt nur Namen, Schnittstellen und Tests fest.

## Global Constraints

- TargetFramework `net8.0` (Cloud Functions Runtime `dotnet8`). Nullable bleibt aktiviert.
- Story-Format 1080 × 1920 px, Ausgabe JPEG mit Qualität 95.
- Farben: Blau `#0B4EA2`, Weiß `#FFFFFF`, Hellblau `#6FA8F0` und `#A9CBF7`. **Kein Gelb und keine anderen Akzente.**
- Schriften Anton und Inter als TTF unter `EventToInsta/Assets/Fonts/`, explizit geladen und nie über Systemschriften.
- Alle Datums- und Zeitlogik läuft in `Europe/Berlin`, alle Texte nutzen die Kultur `de-DE`.
- Bad Laer ist das Team mit `clubId == CLUB_ID` (621). Herren sind Spiele mit `akName == "Senioren"`.
- Gefiltert werden immer: `abgesagt == true`, `verzicht == true` (am Spiel oder an einem der beiden Teams), Anpfiff vor 07:00 und vereinsinterne Spiele (diese mit einer Warnung im Log).
- Gegnernamen: „(ak)“ wird ohne Rücksicht auf Groß- und Kleinschreibung entfernt, danach `Trim()`.
- Umgebungsvariablen: `CLUB_ID` (Default 621), `HOME_PLACE` (Default „Bad Laer“), `UPLOAD_POST_API_KEY`, `UPLOAD_POST_USER` (Default „Dachse“), `STATE_BUCKET`, `SPONSORS` (Default `U12=Böhnke.png;U14=Mann.png;U16=Stockmeyer.webp;U18=Mittelstedt.png`).
- upload-post.com: `POST https://api.upload-post.com/api/upload_photos`, Header `Authorization: Apikey {key}` und `Idempotency-Key`, Felder `user`, `platform[]=instagram`, `media_type=STORIES`, `photos[]` als Datei.
- Kein DI-Framework, keine statischen Service-Klassen.

**Entscheidung abweichend vom Spec-Wortlaut („ein Upload-Aufruf pro Beitrag“):** Die Doku von upload-post.com legt nicht fest, ob mehrere `photos[]` mit `media_type=STORIES` als einzelne Stories erscheinen. Deshalb wird **jedes Bild in einem eigenen Aufruf** hochgeladen, jeweils mit festem `Idempotency-Key` (`{postKey}-{n}`). Der Zustand wird erst gesetzt, wenn alle Bilder eines Beitrags angenommen wurden. Ein Wiederholungslauf schickt dieselben Keys, und upload-post.com verwirft die bereits angenommenen Bilder. Das deckt sich mit dem Ausweichweg, den die Spec selbst vorsieht.

## Review Focus

1. **Gegner ohne Logo:** Die Logo-Antwort ist kürzer als 2000 Byte oder liefert HTTP 404. Erwartet wird ein weißer Kreis mit `ShortName` und kein Absturz. Test dazu in Task 6.
2. **Sehr langer Gegnername:** zum Beispiel „Turn- und Sportverein Grün-Weiß Osnabrück-Haste II (ak)“. Er wird mit „…“ gekürzt und bleibt innerhalb seiner Fläche. Tests in Task 5 (TextFit) und Task 7 (Karte).
3. **Cloud Function in UTC:** 23:30 UTC an einem Samstag ist in Berlin schon Sonntag, 00:30. Der Tag kommt immer aus `IClock` in Berlin-Zeit. Test in Task 3.
4. **Hintergrundfoto mit anderem Seitenverhältnis:** etwa das Querformat-PNG `Bild1.png`. Es wird auf 1080 × 1920 zugeschnitten (cover) und nicht verzerrt. Test in Task 5.
5. **upload-post.com antwortet mit 200, aber `success: false`:** Das gilt als Fehler, der Zustand wird nicht gesetzt, und der Lauf endet mit Fehler. Test in Task 8.

---

## Dateistruktur

```
EventToInsta/
  Domain/        Game.cs, GameMapper.cs
  Data/          BasketballBundDtos.cs, BasketballBundClient.cs (IGameSource, ILogoSource), SampleGameSource.cs
  Time/          Clock.cs (IClock, SystemClock, FixedClock)
  State/         PostedState.cs, IPostedStateStore.cs, FilePostedStateStore.cs, GcsPostedStateStore.cs, NullPostedStateStore.cs
  Planning/      StoryRequests.cs, Chunker.cs, PreviewPlanner.cs, ResultsPlanner.cs
  Rendering/     Theme.cs, Fonts.cs, AssetStore.cs, Primitives.cs, TextFit.cs, StoryRenderer.cs,
                 HerrenStory.cs, JugendStory.cs
  Publishing/    StoryPost.cs, IStoryPublisher.cs, LocalFolderPublisher.cs, UploadPostPublisher.cs
  Jobs/          PreviewJob.cs, ResultsJob.cs
  AppConfig.cs, CompositionRoot.cs, Program.cs, CloudFunction.cs
  Assets/Fonts/  Anton-Regular.ttf, Inter-Bold.ttf, Inter-ExtraBold.ttf, Inter-Medium.ttf
EventToInsta.Tests/   (xUnit, ein Testfile pro Produktionsdatei mit Logik)
docs/deploy.md
```

Gelöscht werden: `MatchesLoader.cs`, `ImageLoader.cs`, `ImageCreator.cs`.

---

### Task 1: Testprojekt, Fachmodell und Mapping

**Files:**
- Create: `EventToInsta.Tests/EventToInsta.Tests.csproj` (xUnit, ProjectReference auf `EventToInsta`, in `EventToInsta.sln` eintragen)
- Create: `EventToInsta/Domain/Game.cs`, `EventToInsta/Domain/GameMapper.cs`, `EventToInsta/Data/BasketballBundDtos.cs`
- Test: `EventToInsta.Tests/Domain/GameMapperTests.cs`

**Interfaces:**
- Produces:
  - `record Team(int PermanentId, string Name, string ShortName, int ClubId)`. `Name` ist schon bereinigt („(ak)“ entfernt).
  - `record Score(int Home, int Guest)` mit `static Score? Parse(string? result)` („57:64“ → (57,64), `null` oder ungültig → `null`)
  - `record Game(int MatchId, string AgeClass, string League, DateTime Kickoff, Team HomeTeam, Team GuestTeam, string? Place, Score? Result, int ClubId)`
    - `Kickoff` ist Berliner Ortszeit mit `DateTimeKind.Unspecified`.
    - Abgeleitet: `bool IsHome`, `bool IsHerren`, `Team Us`, `Team Opponent`, `bool HasResult`, `int? OwnPoints`, `int? OpponentPoints`, `bool? IsWin`.
    - `Game WithPlace(string? place)`
  - DTOs `MatchesResponse`, `MatchDto` (inkl. `HomeTeam`, `GuestTeam` mit `ClubId` und `Verzicht`, `Result`, `Abgesagt`, `Verzicht`, `LigaData`), `MatchInfoResponse` (wie bisher in `MatchesLoader.cs`, ergänzt um die neuen Felder)
  - `static class GameMapper { IReadOnlyList<Game> Map(IEnumerable<MatchDto> matches, int clubId, Action<string>? warn = null) }`

- [ ] **Step 1: Write the failing tests** in `GameMapperTests`. Die Eingaben sind DTOs, die im Test gebaut werden.
  - `Map_AwayGame_ComputesPointsFromBadLaerPerspective`: Heim ist clubId 823, Gast 621, `result "113:16"`. Erwartet `IsHome == false`, `OwnPoints == 16`, `OpponentPoints == 113`, `IsWin == false`.
  - `Map_HomeWin_IsWinTrue`: Heim 621, `"78:64"`. Erwartet `IsWin == true`, `Opponent.ClubId == 823`.
  - `Map_NoResult_HasResultFalse`: `result null`. Erwartet `HasResult == false` und `IsWin == null`.
  - `Map_FiltersCancelledVerzichtEarlyAndDerby`: vier Spiele, eins mit `abgesagt`, eins mit `verzicht`, eins mit Anpfiff „06:59“ und eins mit beiden clubId 621. Erwartet ein leeres Ergebnis, `warn` wurde genau einmal aufgerufen (Derby).
  - `Map_CleansAkSuffix`: `"SC Melle I (aK)"` → `Opponent.Name == "SC Melle I"`
  - `Map_Senioren_IsHerren`: `akName "Senioren"` → `IsHerren == true`, „U16“ → false
  - `Map_ParsesKickoffAsLocal`: „2026-11-28“ / „19:30“ → `new DateTime(2026,11,28,19,30,0)` mit `Kind == Unspecified`
  - `ScoreParse_Invalid_ReturnsNull`: `"abc"`, `""` und `"12:"` ergeben jeweils `null`.
- [ ] **Step 2: Run tests, verify they fail.** `dotnet test` → Kompilierfehler oder FAIL.
- [ ] **Step 3: Implement** `Game`, `Team`, `Score`, DTOs und `GameMapper` wie unter Interfaces beschrieben. Datum parsen mit `DateTime.ParseExact($"{date} {time}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)`.
- [ ] **Step 4: Run tests, verify they pass.** `dotnet test` → alle PASS.
- [ ] **Step 5: Commit.** `git add EventToInsta.Tests EventToInsta/Domain EventToInsta/Data EventToInsta.sln && git commit -m "feat: Fachmodell Game und Mapping aus basketball-bund DTOs"`

### Task 2: Datenquelle basketball-bund.net

**Files:**
- Create: `EventToInsta/Data/BasketballBundClient.cs`
- Test: `EventToInsta.Tests/Data/BasketballBundClientTests.cs`, Fixture `EventToInsta.Tests/Data/actualmatches.json`. Das ist ein gekürzter echter Response mit drei Spielen: ein Spiel mit Ergebnis, ein abgesagtes und ein Herrenspiel ohne Ergebnis. Die Struktur stammt aus der Spec, Abschnitt Rahmenbedingungen.

**Interfaces:**
- Consumes: `GameMapper.Map`, DTOs (Task 1)
- Produces:
  - `interface IGameSource { Task<IReadOnlyList<Game>> LoadGamesAsync(CancellationToken ct = default); }`
  - `interface ILogoSource { Task<byte[]?> LoadTeamLogoAsync(int teamPermanentId, CancellationToken ct = default); }`
  - `sealed class BasketballBundClient(HttpClient http, int clubId) : IGameSource, ILogoSource`
    - Spiele laden: `rest/club/id/{clubId}/actualmatches?justHome=false&rangeDays=7`
    - Danach für jedes gemappte Spiel den Ort: `rest/match/id/{id}/matchInfo` → `spielfeld.ort`
    - Logo: `media/team/{id}/logo`. Bei weniger als 2000 Byte oder einem HTTP-Fehler `null`.

- [ ] **Step 1: Write failing tests.** Verwendet wird ein `HttpMessageHandler`-Fake, der per URL-Pfad antwortet.
  - `LoadGames_MapsFixtureAndLoadsPlaces`: Erwartet zwei Spiele (das abgesagte fehlt). Das Spiel mit Ort-Antwort „Dissen“ hat `Place == "Dissen"`.
  - `LoadTeamLogo_SmallResponse_ReturnsNull`: Eine Antwort mit 500 Byte ergibt `null`.
  - `LoadTeamLogo_NotFound_ReturnsNull`: HTTP 404 ergibt `null`.
- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement** `BasketballBundClient` mit `System.Net.Http.Json` und camelCase-JSON. Flurl entfällt hier, damit der Fake-Handler direkt passt. Das Paket `Flurl.Http` wird aus der csproj entfernt, sobald `MatchesLoader.cs` gelöscht ist (Task 10).
- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Commit.** `git commit -m "feat: BasketballBundClient als IGameSource/ILogoSource"`

### Task 3: Uhr und Zustand

**Files:**
- Create: `EventToInsta/Time/Clock.cs`, `EventToInsta/State/PostedState.cs`, `IPostedStateStore.cs`, `FilePostedStateStore.cs`, `GcsPostedStateStore.cs`, `NullPostedStateStore.cs`
- Modify: `EventToInsta/EventToInsta.csproj` (PackageReference `Google.Cloud.Storage.V1`)
- Test: `EventToInsta.Tests/Time/ClockTests.cs`, `EventToInsta.Tests/State/PostedStateTests.cs`, `FilePostedStateStoreTests.cs`

**Interfaces:**
- Produces:
  - `interface IClock { DateTime Now { get; } }`. Liefert Berliner Ortszeit mit `Kind Unspecified`.
  - `sealed class SystemClock(Func<DateTimeOffset>? utcNow = null) : IClock`. Nutzt `TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")`.
  - `sealed class FixedClock(DateTime now) : IClock`
  - `record PostedState(string? PreviewWeek, DateOnly? Date, IReadOnlyList<int> HerrenMatchIds, bool JugendPosted)`
    - `static PostedState Empty`
    - `PostedState ForDay(DateOnly today)`: Weicht `Date` von `today` ab, ergibt das `Date = today`, leere IDs und `JugendPosted = false`. `PreviewWeek` bleibt erhalten.
    - `PostedState WithHerren(int matchId)`, `WithJugendPosted()`, `WithPreviewWeek(string week)`
    - `static string IsoWeek(DateOnly day)` → `"2026-W48"`
  - `interface IPostedStateStore { Task<PostedState> LoadAsync(CancellationToken ct = default); Task SaveAsync(PostedState state, CancellationToken ct = default); }`
  - `FilePostedStateStore(string path)`: Fehlt die Datei, gilt `Empty`.
  - `GcsPostedStateStore(StorageClient client, string bucket)`: Objekt `posted.json`, bei 404 `Empty`.
  - `NullPostedStateStore`: lädt `Empty` und verwirft jedes Speichern.
  - JSON-Format exakt wie in der Spec: `{ "previewWeek", "date": "yyyy-MM-dd", "herrenMatchIds", "jugendPosted" }`

- [ ] **Step 1: Write failing tests.**
  - `SystemClock_UtcLateSaturday_IsSundayInBerlin`: Aus `2026-11-28T23:30Z` wird `Now == 2026-11-29 00:30`.
  - `SystemClock_Summer_UsesCest`: Aus `2026-07-04T10:00Z` wird `Now.Hour == 12`.
  - `ForDay_OtherDate_ResetsDailyButKeepsPreviewWeek`
  - `ForDay_SameDate_KeepsEverything`
  - `IsoWeek_2026_11_23_IsW48` und `IsoWeek_2027_01_01_IsW53OfPrevYear`. Das zweite gilt, wenn der 01.01.2027 laut `ISOWeek` zu 2026-W53 gehört. Den Erwartungswert über `ISOWeek.GetYear` und `GetWeekOfYear` gegenprüfen.
  - `FileStore_RoundTrip_PreservesAllFields`, `FileStore_MissingFile_ReturnsEmpty`
- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement.** `IsoWeek` mit `System.Globalization.ISOWeek`. Die GCS-Klasse bekommt keinen Unit-Test, sie ist ein dünner Adapter und wird in Task 11 manuell geprüft.
- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Commit.** `git commit -m "feat: Berlin-Uhr und posted.json-Zustand (Datei, GCS)"`

### Task 4: Planer (was wird gepostet)

**Files:**
- Create: `EventToInsta/Planning/StoryRequests.cs`, `Chunker.cs`, `PreviewPlanner.cs`, `ResultsPlanner.cs`
- Test: `EventToInsta.Tests/Planning/ChunkerTests.cs`, `PreviewPlannerTests.cs`, `ResultsPlannerTests.cs`. Dazu ein Test-Helper `GameFactory` für knappe `Game`-Erzeugung.

**Interfaces:**
- Consumes: `Game`, `PostedState`
- Produces:
  - `abstract record StoryRequest(string FileName)` mit den Ableitungen:
    - `HerrenPreview(Game Game)`
    - `JugendPreview(bool Home, DateOnly WeekStart, DateOnly WeekEnd, IReadOnlyList<Game> Games, int Part, int PartCount)`
    - `HerrenResult(Game Game)`
    - `JugendResult(DateOnly Day, IReadOnlyList<Game> Games, int Part, int PartCount)`
    - Dateinamen sind eindeutig: `herren-preview-{matchId}.jpg`, `jugend-preview-{heim|auswaerts}-{part}.jpg`, `herren-result-{matchId}.jpg`, `jugend-result-{yyyy-MM-dd}-{part}.jpg`.
  - `static class Chunker { IReadOnlyList<IReadOnlyList<T>> SplitEvenly<T>(IReadOnlyList<T> items, int maxPerPart) }`: Anzahl Teile = ⌈n/max⌉. Die Größen unterscheiden sich höchstens um 1, vordere Teile sind größer.
  - `static class PreviewPlanner { IReadOnlyList<StoryRequest> Plan(IReadOnlyList<Game> games, DateOnly today, int maxCardsPerImage) }`
    - Zeitraum `today` bis Sonntag derselben Woche
    - Reihenfolge: Herren-Heim, dann Herren-Auswärts (je chronologisch), dann Jugend-Heim-Teile, dann Jugend-Auswärts-Teile
  - `record ResultsPlan(IReadOnlyList<HerrenResult> Herren, IReadOnlyList<JugendResult> Jugend)`
  - `static class ResultsPlanner { ResultsPlan Plan(IReadOnlyList<Game> games, DateTime now, PostedState state, int maxCardsPerImage) }`. `state` ist bereits per `ForDay` auf heute gesetzt.
  - Konstante `ResultsPlanner.FinalHour = 23`

- [ ] **Step 1: Write failing tests.**
  - Chunker: `7,max6 → [4,3]`, `6,max6 → [6]`, `13,max6 → [5,4,4]`, `0 → []`
  - `Preview_SplitsHerrenHomeAwayAndJugendGroups`: Herren heim, Herren auswärts, 2 Jugend heim, 1 Jugend auswärts. Erwartet 4 Requests in der oben festgelegten Reihenfolge.
  - `Preview_IgnoresGamesOutsideWeek`: Ein Spiel am nächsten Montag fehlt, ein Spiel heute um 08:00, also vor dem Laufzeitpunkt, ist enthalten.
  - `Preview_EmptyGroupsProduceNoRequest`: Nur Jugend heim ergibt genau einen `JugendPreview` mit `Home == true`.
  - `Preview_SevenJugendHome_TwoPartsFourAndThree`: `maxCardsPerImage = 6`
  - `Results_HerrenWithResult_NotYetPosted_IsPlanned`
  - `Results_HerrenAlreadyInState_Skipped`
  - `Results_JugendNotAllDone_Before23_NothingPlanned`: um 18:00, 1 von 2 mit Ergebnis
  - `Results_JugendAllDone_Planned`: um 15:00
  - `Results_JugendAt23_PostsOnlyThoseWithResult`
  - `Results_JugendAt23_NoneWithResult_NothingPlanned`
  - `Results_JugendAlreadyPosted_NothingPlanned`
  - `Results_IgnoresYesterdaysGames`
  - `Results_HerrenPendingDoesNotBlockJugend`: Herren ohne Ergebnis, Jugend komplett. Erwartet Jugend geplant, Herren leer.
- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement** die Planer als reine Funktionen ohne I/O.
- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Commit.** `git commit -m "feat: Vorschau- und Ergebnisplaner"`

### Task 5: Rendering-Grundlagen (Theme, Schriften, Assets, Bausteine)

**Files:**
- Create: `EventToInsta/Rendering/Theme.cs`, `Fonts.cs`, `AssetStore.cs`, `Primitives.cs`, `TextFit.cs`
- Create: `EventToInsta/Assets/Fonts/*.ttf`. Die Dateien kommen aus dem Repo `google/fonts`, beide unter der OFL: `ofl/anton/Anton-Regular.ttf` und `ofl/inter/Inter[opsz,wght].ttf`. Wenn RichTextKit mit der variablen Schrift nicht sauber auf die Stärken 500, 700 und 800 abbildet, kommen stattdessen statische Inter-Schnitte aus `rsms/inter` (Releases) hinein. Die `OFL.txt` wird mitgelegt.
- Modify: `EventToInsta/EventToInsta.csproj`. Alle `Content Remove`-Einträge entfallen. `Images\**` und `Assets\**` werden mit `PreserveNewest` kopiert.
- Test: `EventToInsta.Tests/Rendering/TextFitTests.cs`, `PrimitivesTests.cs`, `AssetStoreTests.cs`

**Interfaces:**
- Produces:
  - `static class Theme`
    - `Blue`, `White`, `LightBlue` (#6FA8F0), `PaleBlue` (#A9CBF7), `Grey` (#B8C4D6), `Width = 1080`, `Height = 1920`, `JpegQuality = 95`
    - Schrift-Konstanten `Display = "Anton"` und `Body = "Inter"`
    - `CultureInfo German`
  - `static class Fonts { void Register(string fontsDirectory) }`. Registriert einen eigenen RichTextKit-`FontMapper`, der für „Anton“ und „Inter“ (nach Gewicht) die TTF-Typefaces liefert und für alles andere eine Exception wirft. Mehrfacher Aufruf ist erlaubt (idempotent).
  - `sealed class AssetStore(string imagesDirectory, IReadOnlyDictionary<string,string> sponsorByAge, Random random)`
    - `SKBitmap RandomHerrenPhoto()`
    - `SKBitmap RandomJugendPhoto()`: Fehlt der Ordner `Jugend` oder ist er leer, wird `Herren` verwendet.
    - `SKBitmap ClubLogo()` (`Logos/Dachse.png`)
    - `SKBitmap? SponsorLogo(string ageClass)`
  - `static class Primitives`
    - `void DrawCover(SKCanvas c, SKBitmap photo, SKRect target)`: skaliert auf Füllgröße, zentriert und schneidet zu.
    - `void DrawLogoOrBadge(SKCanvas c, SKBitmap? logo, string shortName, SKRect box, SKColor badgeFill, SKColor badgeText)`
    - `void DrawContained(SKCanvas c, SKBitmap bmp, SKRect box)`: seitenverhältnistreu einpassen
  - `static class TextFit`
    - `TextBlock Line(string text, Style style, float maxWidth)`: eine Zeile, `MaxLines = 1`, bei Überlänge mit „…“ gekürzt. `MeasuredWidth <= maxWidth` ist garantiert.
    - `TextBlock Centered(...)` analog, zentriert

- [ ] **Step 1: Write failing tests.**
  - `Fonts_Register_AntonResolvesToBundledTypeface`: Der `FamilyName` des Typeface ist „Anton“.
  - `TextFit_LongOpponent_FitsWidthAndEndsWithEllipsis`: Text „Turn- und Sportverein Grün-Weiß Osnabrück-Haste II“, Größe 56, maxWidth 600. Erwartet `MeasuredWidth <= 600` und einen sichtbaren Text, der auf „…“ endet (über `TextBlock`-Lines oder Ellipsis-Flag prüfen).
  - `TextFit_ShortText_Unchanged`
  - `DrawCover_LandscapePhoto_FillsTargetWithoutDistortion`: Ein 2000×1000-Bitmap mit linker Hälfte rot und rechter Hälfte grün wird in ein 1080×1920-Ziel gezeichnet. Erwartet: Pixel (0,960) und (1079,960) sind nicht transparent, und die Mitte liegt an der Farbgrenze (die Pixel links und rechts der Mitte unterscheiden sich).
  - `AssetStore_EmptyJugendFolder_FallsBackToHerren`: Testordner mit `Herren/x.png`, ohne `Jugend`
  - `AssetStore_UnknownAge_NoSponsor`
- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Commit.** `git commit -m "feat: Rendering-Grundlagen, gebündelte Schriften, AssetStore"`

### Task 6: Herren-Stories (Vorschau und Ergebnis, Stil „Matchday“)

**Files:**
- Create: `EventToInsta/Rendering/HerrenStory.cs`, `EventToInsta/Rendering/StoryRenderer.cs`
- Test: `EventToInsta.Tests/Rendering/HerrenStoryTests.cs`

**Interfaces:**
- Consumes: `Theme`, `Primitives`, `TextFit`, `AssetStore`, `ILogoSource`, `HerrenPreview`/`HerrenResult`
- Produces:
  - `record RenderedStory(string FileName, byte[] Jpeg)`
  - `sealed class StoryRenderer(AssetStore assets, ILogoSource logos, string homePlace) { Task<RenderedStory> RenderAsync(StoryRequest request, CancellationToken ct = default); }`. Wechselt per Typ. Die Jugend-Zweige kommen in Task 7 dazu und werfen bis dahin `NotSupportedException`.
  - `static class HerrenStory { void DrawPreview(SKCanvas c, HerrenPreview r, SKBitmap photo, SKBitmap clubLogo, SKBitmap? opponentLogo); void DrawResult(SKCanvas c, HerrenResult r, SKBitmap photo, SKBitmap clubLogo, SKBitmap? opponentLogo); }`

Layout nach Spec, Abschnitt *Herren* und `herren-b-v3.html`/`results-v1.html`. Die Mockups sind 300 px breit, Skalierfaktor **3,6**. Feste Texte:
- Etikett „HEIMSPIEL“, „AUSWÄRTSSPIEL“ oder „ENDSTAND“ (bei Sieg weißer Grund mit blauer Schrift)
- „GAME“ / „DAY“
- Zeile `"{dddd}, {dd.MM.} · {HH:mm} UHR"` in Großbuchstaben, auswärts zusätzlich `" · {ORT}"`
- „SIEG!“
- Ergebniszeile `"{dddd}, {dd.MM.} · {LIGA}"` in Großbuchstaben
- Fußzeile `"{HEIM}\nVS {GAST}"`, Bad Laer als „HERREN“

Der Fußtext wird mit `TextFit` auf den Raum zwischen den Logos begrenzt.

- [ ] **Step 1: Write failing tests.** Fake-`ILogoSource` und ein Testbild-Ordner.
  - `Preview_ProducesJpeg1080x1920`: Dekodieren ergibt 1080×1920.
  - `Preview_OpponentWithoutLogo_DoesNotThrow`: Logo `null`, Erwartung: Badge.
  - `Result_LongOpponentName_DoesNotThrow`
  - `Preview_Away_FileNameAndNoException`
- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement** `HerrenStory` und `StoryRenderer` (Herren-Zweige).
- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Visual check.** Ein temporärer Test schreibt vier Bilder nach `%TEMP%/eventtoinsta-visual/`: Heim, Auswärts mit langem Namen, Sieg, Niederlage ohne Gegnerlogo. Die Bilder mit dem Read-Tool ansehen und gegen die Mockups prüfen. **Nichts darf überlappen.** Danach den Test wieder entfernen, `--sample` in Task 10 ersetzt ihn.
- [ ] **Step 6: Commit.** `git commit -m "feat: Herren-Stories im Matchday-Stil"`

### Task 7: Jugend-Stories (Vorschau und Ergebnis, Stil „Infokarte“)

**Files:**
- Create: `EventToInsta/Rendering/JugendStory.cs`
- Modify: `EventToInsta/Rendering/StoryRenderer.cs` (Jugend-Zweige)
- Test: `EventToInsta.Tests/Rendering/JugendStoryTests.cs`

**Interfaces:**
- Consumes: wie Task 6, dazu `JugendPreview`/`JugendResult` und `AssetStore.SponsorLogo`
- Produces:
  - `static class JugendStory { void DrawPreview(SKCanvas c, JugendPreview r, SKBitmap photo, Func<string, SKBitmap?> sponsorFor, string homePlace); void DrawResult(SKCanvas c, JugendResult r, SKBitmap photo); }`
  - `const int JugendStory.MaxCards = 6`. Die Planer bekommen diesen Wert aus dem `CompositionRoot`.

Feste Texte nach Spec, Abschnitt *Jugend*:
- Kopfbereich:
  - „SV BAD LAER · JUGEND“
  - „HEIMSPIELE“, „AUSWÄRTSSPIELE“ oder „ERGEBNISSE“
  - `"Diese Woche · {dd.}–{dd.MM.}"` bzw. `"{dddd}, {dd.MM.}"`
  - bei `PartCount > 1` zusätzlich `" ({Part}/{PartCount})"`
- Karte:
  - Gegnerzeile `"vs {Name}"` bzw. `"@ {Name}"`
  - Vorschau-Infozeile `"{ddd} {dd.MM.} · {HH:mm} Uhr"`, zusätzlich `" · in {Ort}"`, wenn `Place` nicht null ist und ungleich `homePlace` (Vergleich ohne Groß-/Kleinschreibung)
  - Sponsor „unterstützt von“ und Logo
  - Ergebnis „Heim“ bzw. „Auswärts“, Spielstand `"{Own}:{Opp}"`, „SIEG“ bzw. „NIEDERLAGE“, Rand blau bzw. grau
- Fuß: „KOMMT VORBEI & FEUERT UNS AN!“ bzw. „STARK GESPIELT, TEAMS!“

`MaxCards = 6` muss im Layout zwischen Kopf und Fuß Platz haben, ohne Überlappung.

- [ ] **Step 1: Write failing tests.**
  - `Preview_SixCards_ProducesJpeg1080x1920`
  - `Preview_LongNameWithSponsor_DoesNotThrow`: Gegnername mit 60 Zeichen und Sponsor
  - `Result_MixedHomeAway_DoesNotThrow`
  - `Preview_AwayPlaceEqualsHomePlace_NoPlaceSuffix`: Die Textaufbereitung als `internal static string PreviewInfoLine(Game g, string homePlace)` herauslösen und direkt testen. Erwartet „Sa 28.11. · 10:00 Uhr“ ohne Zusatz, mit Ort „Dissen“ dann „Sa 28.11. · 10:00 Uhr · in Dissen“.
- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Visual check** wie in Task 6: Vorschau mit 6 Karten, davon 2 mit Sponsor und 1 mit fremdem Ort, sowie Ergebnis mit 4 Karten (gemischt Sieg und Niederlage, Heim und Auswärts). Ansehen und auf Überlappungen prüfen.
- [ ] **Step 6: Commit.** `git commit -m "feat: Jugend-Stories im Infokarten-Stil"`

### Task 8: Publisher

**Files:**
- Create: `EventToInsta/Publishing/StoryPost.cs`, `IStoryPublisher.cs`, `LocalFolderPublisher.cs`, `UploadPostPublisher.cs`
- Test: `EventToInsta.Tests/Publishing/UploadPostPublisherTests.cs`, `LocalFolderPublisherTests.cs`

**Interfaces:**
- Consumes: `RenderedStory`
- Produces:
  - `record StoryPost(string Key, IReadOnlyList<RenderedStory> Images)`. Die Keys sind `preview-{IsoWeek}`, `herren-result-{matchId}` und `jugend-result-{yyyy-MM-dd}`.
  - `interface IStoryPublisher { Task PublishAsync(StoryPost post, CancellationToken ct = default); }`
  - `LocalFolderPublisher(string directory)`: schreibt `Images[i].FileName` in den Ordner und überschreibt vorhandene Dateien.
  - `UploadPostPublisher(HttpClient http, string apiKey, string user)`
    - pro Bild ein `POST https://api.upload-post.com/api/upload_photos`
    - Header `Authorization: Apikey {apiKey}` und `Idempotency-Key: {post.Key}-{i+1}`
    - Multipart-Felder `user`, `platform[]` = `instagram`, `media_type` = `STORIES`, `photos[]` = JPEG-Datei (Dateiname `FileName`, `image/jpeg`)
    - Erfolg nur bei 2xx **und** `success == true` im JSON. Sonst `UploadFailedException`, mit Status und Body.
  - `sealed class UploadFailedException(string message) : Exception`

- [ ] **Step 1: Write failing tests.** Ein `HttpMessageHandler`-Fake zeichnet die Requests auf.
  - `Publish_TwoImages_TwoRequestsWithIdempotencyKeys`: Erwartet die Keys `jugend-result-2026-11-21-1` und `-2`.
  - `Publish_SendsRequiredFormFields`: `user=Dachse`, `platform[]=instagram`, `media_type=STORIES`, `photos[]` mit Dateiinhalt gleich den JPEG-Bytes, Authorization `Apikey test-key`
  - `Publish_200WithSuccessFalse_Throws`
  - `Publish_500_Throws`
  - `Publish_SecondImageFails_ThrowsAfterFirstSent`
  - `LocalFolder_WritesAllFiles`
- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Commit.** `git commit -m "feat: Publisher für upload-post.com und lokalen Ordner"`

### Task 9: Jobs

**Files:**
- Create: `EventToInsta/Jobs/PreviewJob.cs`, `EventToInsta/Jobs/ResultsJob.cs`
- Test: `EventToInsta.Tests/Jobs/PreviewJobTests.cs`, `ResultsJobTests.cs`. Dafür Fakes für `IGameSource`, einen In-Memory-`IPostedStateStore` und einen Publisher, der aufzeichnet und auf Wunsch fehlschlägt. Dazu ein `IStoryRendererFake`. Deshalb wird in diesem Task `interface IStoryRenderer { Task<RenderedStory> RenderAsync(StoryRequest r, CancellationToken ct = default); }` eingeführt und von `StoryRenderer` implementiert.

**Interfaces:**
- Consumes: alles aus den Tasks 2–8
- Produces:
  - `sealed class PreviewJob(IGameSource games, IStoryRenderer renderer, IStoryPublisher publisher, IPostedStateStore state, IClock clock, int maxCards, Action<string> log) { Task RunAsync(CancellationToken ct = default); }`
  - `sealed class ResultsJob(...)` mit gleicher Signatur
  - Ablauf `PreviewJob`:
    1. Zustand laden.
    2. Ist `PreviewWeek == IsoWeek(today)`, wird geloggt und beendet.
    3. Planen. Ohne Requests wird geloggt und beendet, ohne Zustand zu setzen.
    4. **Alle Bilder rendern.**
    5. Ein `StoryPost("preview-{week}")` veröffentlichen.
    6. `WithPreviewWeek` speichern.
  - Ablauf `ResultsJob`:
    1. Zustand laden und `ForDay(today)` anwenden.
    2. Planen.
    3. Für jedes Herren-Ergebnis: rendern, als `herren-result-{id}` veröffentlichen, `WithHerren` speichern.
    4. Dann Jugend: alle Teile rendern, als `jugend-result-{date}` veröffentlichen, `WithJugendPosted` speichern.
    5. Fehler eines Beitrags werden gesammelt, die übrigen laufen weiter. Am Ende wird `AggregateException` geworfen, falls es Fehler gab.

- [ ] **Step 1: Write failing tests.**
  - `Preview_AlreadyPostedThisWeek_PublishesNothing`
  - `Preview_RendersAllBeforePublishing`: Der Renderer wirft beim zweiten Bild. Erwartet: Der Publisher wurde nie aufgerufen, und der Zustand ist unverändert.
  - `Preview_PublishFails_StateNotSaved_Throws`
  - `Preview_NoGames_NoPublish_NoStateSaved`
  - `Preview_Success_SavesWeek`
  - `Results_HerrenPublished_StateSavedBeforeJugend`: Der Jugend-Publish wirft. Erwartet: Die Herren-ID ist gespeichert, `JugendPosted == false`, und es wird geworfen.
  - `Results_SecondRunSameDay_DoesNotRepostHerren`
  - `Results_NewDay_ResetsDailyState`
- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run, verify PASS.**
- [ ] **Step 5: Commit.** `git commit -m "feat: PreviewJob und ResultsJob mit Zustand und Fehlerbehandlung"`

### Task 10: Einstiegspunkte, Konfiguration, Beispieldaten, Altcode entfernen

**Files:**
- Create: `EventToInsta/AppConfig.cs`, `EventToInsta/CompositionRoot.cs`, `EventToInsta/Data/SampleGameSource.cs`
- Modify: `EventToInsta/Program.cs` (komplett ersetzt), `EventToInsta/CloudFunction.cs` (wird `public`), `EventToInsta/EventToInsta.csproj` (entfernt `Flurl.Http` und `InstagramApiSharp`, falls ungenutzt, sowie `Dropbox.Api`)
- Delete: `EventToInsta/MatchesLoader.cs`, `ImageLoader.cs`, `ImageCreator.cs`
- Test: `EventToInsta.Tests/AppConfigTests.cs`, `EventToInsta.Tests/ProgramArgsTests.cs`

**Interfaces:**
- Produces:
  - `record AppConfig(int ClubId, string HomePlace, string? UploadPostApiKey, string UploadPostUser, string? StateBucket, IReadOnlyDictionary<string,string> Sponsors)`
    - `static AppConfig FromEnvironment(Func<string, string?> getEnv)`. Liest die Defaults aus den Global Constraints. `SPONSORS` hat das Format `AK=Datei;AK=Datei`.
  - `record RunOptions(string Job, bool DryRun, bool Sample, DateTime? Now)`
    - `static RunOptions Parse(string[] args)`. Gültige Jobs sind `preview` und `results`. Bei einem unbekannten Job oder fehlenden Argumenten wird `ArgumentException` mit Usage-Text geworfen. `--now` nimmt `yyyy-MM-ddTHH:mm` an.
  - `static class CompositionRoot { Func<CancellationToken, Task> Build(AppConfig cfg, RunOptions opt, Action<string> log) }`. Bestimmt die Umsetzungen:
    - Datenquelle: `--sample` → `SampleGameSource`, sonst `BasketballBundClient`
    - Zustand: `--dry-run` → `NullPostedStateStore`; ist `STATE_BUCKET` gesetzt → `GcsPostedStateStore`; sonst `FilePostedStateStore("posted.json")`
    - Publisher: `--dry-run` → `LocalFolderPublisher("Output")`, sonst `UploadPostPublisher`. Fehlt der API-Key ohne `--dry-run`, gibt es einen klaren Fehler.
    - Uhr: `--now` → `FixedClock`, sonst `SystemClock`
    - Rendering: `Fonts.Register` wird hier aufgerufen.
  - `SampleGameSource(IClock clock)` erzeugt Spiele relativ zu `clock.Now`:
    - Diese Woche: 1 Herren-Heimspiel und 1 Herren-Auswärtsspiel, ein Gegner ohne Logo (`PermanentId` 0, der Logo-Fake liefert `null`)
    - 7 Jugend-Heimspiele, davon einer mit einem 60 Zeichen langen Namen und einer in einem fremden Ort
    - 2 Jugend-Auswärtsspiele
    - Heute: Herren mit Ergebnis (Sieg), 4 Jugend mit Ergebnis (gemischt), 1 Jugend ohne Ergebnis
    - Im `--sample`-Modus kommt `ILogoSource` als Fake, der für alle IDs außer 0 das Vereinslogo liefert.
  - `Program.Main(string[] args)` gibt `int` zurück: 0 bei Erfolg, 1 bei Fehler (Fehlermeldung auf stderr).
  - `CloudFunction.HandleAsync`:
    - `?job=preview|results` → 200 „ok“
    - fehlender oder unbekannter Job → 400
    - Exception → 500 mit Meldung, Fehler auch geloggt
    - nie `--dry-run`
    - Der Font-Pfad ist `Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts")`.

- [ ] **Step 1: Write failing tests.**
  - `FromEnvironment_Defaults`: leere Umgebung → ClubId 621, HomePlace „Bad Laer“, User „Dachse“, U14 → „Mann.png“
  - `FromEnvironment_SponsorsOverride`
  - `Parse_PreviewDryRunSampleNow`
  - `Parse_UnknownJob_Throws`
- [ ] **Step 2: Run, verify FAIL.**
- [ ] **Step 3: Implement** und Altcode löschen. `dotnet build` muss ohne Warnungen zu den neuen Dateien bauen.
- [ ] **Step 4: Run all tests.** `dotnet test` → alle PASS.
- [ ] **Step 5: End-to-end lokal mit Beispieldaten.**
  - `dotnet run --project EventToInsta -- preview --dry-run --sample` → `Output/` enthält `herren-preview-*.jpg` (2) und `jugend-preview-heim-1.jpg`, `-2.jpg` (4+3) sowie `jugend-preview-auswaerts-1.jpg`.
  - `dotnet run --project EventToInsta -- results --dry-run --sample --now <heute>T23:00` → `herren-result-*.jpg` und `jugend-result-*-1.jpg` (4 Karten).
  - Alle Bilder mit dem Read-Tool ansehen. Nichts überlappt, langer Name gekürzt, Badge statt Logo, fremder Ort sichtbar.
- [ ] **Step 6: End-to-end mit echten Daten, ohne Upload.** `dotnet run --project EventToInsta -- preview --dry-run` und `results --dry-run`. Erwartet Exit-Code 0, die Bilder ansehen.
- [ ] **Step 7: Commit.** `git commit -m "feat: CLI, Cloud Function, Konfiguration und Beispieldaten; Altcode entfernt"`

### Task 11: Bereitstellung

**Files:**
- Create: `docs/deploy.md`

**Inhalt** (gcloud-Befehle, Region `europe-west3`):
1. Bucket anlegen (`gcloud storage buckets create gs://<bucket> --location=europe-west3`).
2. Function deployen: `gcloud functions deploy eventtoinsta --gen2 --runtime=dotnet8 --region=europe-west3 --source=EventToInsta --entry-point=EventToInsta.CloudFunction --trigger-http --no-allow-unauthenticated --memory=512Mi --timeout=300s --set-env-vars=STATE_BUCKET=<bucket>,UPLOAD_POST_USER=Dachse --set-secrets=UPLOAD_POST_API_KEY=<secret>:latest`
3. Service-Account der Function erhält `roles/storage.objectAdmin` auf den Bucket.
4. Scheduler:
   - `eventtoinsta-preview`: `--schedule="0 8 * * 1" --time-zone="Europe/Berlin" --max-retry-attempts=0`, URI `…?job=preview`, OIDC-Token eines Invoker-Service-Accounts
   - `eventtoinsta-results`: `--schedule="0 12-23 * * *" --time-zone="Europe/Berlin" --max-retry-attempts=0`, URI `…?job=results`
5. Manuell auslösen: `gcloud scheduler jobs run eventtoinsta-preview`. Logs: `gcloud functions logs read eventtoinsta`.
6. Hinweis: Die alte Umgebungsvariable `upload_posts_api_key` ersetzt jetzt `UPLOAD_POST_API_KEY`.

- [ ] **Step 1: `docs/deploy.md` schreiben.**
- [ ] **Step 2: Manuelle Abnahme mit dem Auftraggeber (nicht automatisierbar).** Einen echten Test-Post auslösen, möglichst auf einem Test-Profil von upload-post.com. Prüfen, ob die Stories einzeln erscheinen und ob ein zweiter Lauf nichts doppelt postet. `posted.json` im Bucket ansehen.
- [ ] **Step 3: Commit.** `git commit -m "docs: Bereitstellung Cloud Function und Scheduler"`
