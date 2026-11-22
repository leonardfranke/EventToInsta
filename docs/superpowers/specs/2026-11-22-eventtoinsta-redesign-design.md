# EventToInsta – Überarbeitung: Vorschau- und Ergebnis-Stories

Datum: 2026-11-22
Status: Entwurf zur Freigabe

## Zweck

Der SV Bad Laer (Basketball, Verein 621 bei basketball-bund.net) soll auf Instagram automatisch Stories bekommen:

1. **Montags eine Vorschau auf die Woche:** Welche Spiele stehen an?
2. **Abends die Ergebnisse:** Wie sind die Spiele des Tages ausgegangen?

Die Herren bekommen dabei immer eigene Beiträge, einen pro Spiel. Die Jugendmannschaften werden zu Sammelbeiträgen zusammengefasst.

Heute erzeugt das Tool nur Vorschaubilder für Heimspiele. Der Upload ist durch ein `return;` abgeschaltet und wäre in der jetzigen Form auch fehlerhaft. Das Ergebnisposting fehlt ganz. Mit dem Aussehen und dem Codeaufbau ist der Auftraggeber nicht zufrieden: Alles ist statisch, die Zeichnung steht in einer langen Methode, und die Werte sind fest im Code verdrahtet.

## Ziele

- Vier Story-Typen in einem neuen, einheitlichen Design (siehe *Aussehen*)
  - Vorschau Herren: ein Bild pro Spiel, Heim- und Auswärtsspiele
  - Vorschau Jugend: getrennt nach Heim- und Auswärtsspielen, bei vielen Spielen auf mehrere Bilder verteilt
  - Ergebnis Herren: ein Bild pro Spiel
  - Ergebnis Jugend: Heim und Auswärts gemeinsam, bei vielen Spielen auf mehrere Bilder verteilt
- Vollautomatischer Betrieb als Google Cloud Function, ausgelöst durch Cloud Scheduler
- Funktionierender Upload als Instagram-Story über upload-post.com
- Kein doppeltes Posten
- Ein klar getrennter, testbarer Codeaufbau: Daten laden → auswählen → zeichnen → veröffentlichen
- Ein lokaler Modus, der nur Bilder erzeugt, mit echten Daten oder mit Beispieldaten

## Nicht-Ziele

- Keine anderen Vereine und keine Mehrmandantenfähigkeit. Die Vereins-ID kommt aus der Konfiguration, mehr nicht.
- Keine anderen Plattformen als Instagram-Stories und keine Feed-Posts
- Kein Nachposten von Ergebnissen, die erst nach dem Tages-Post eingetragen werden
- Keine Tabellen, Statistiken oder Boxscores
- Kein Web-Editor für Vorlagen

## Anti-Ziele (das wäre ein Fehlschlag, auch wenn es technisch läuft)

- Dieselbe Story wird doppelt gepostet.
- Gegnernamen laufen aus dem Bild oder überlappen andere Elemente. Elemente überlagern sich generell. Das war ein konkreter Kritikpunkt in den Entwürfen.
- Es werden falsche Ergebnisse gepostet, etwa mit vertauschtem Heim und Gast, oder abgesagte Spiele erscheinen als Spiel.
- Der Betrieb braucht regelmäßige Handarbeit.

## Rahmenbedingungen

- .NET 8, SkiaSharp und Topten.RichTextKit sind bereits im Einsatz und bleiben.
- Laufzeitumgebung ist eine Google Cloud Function (Functions Framework, `IHttpFunction`). Das Dateisystem ist schreibgeschützt bis auf `/tmp`, und es bleibt kein Zustand zwischen zwei Aufrufen erhalten.
- Die Cloud Function läuft in UTC. Alle Tages- und Uhrzeitlogik muss in **Europe/Berlin** gerechnet werden. Die Zeiten von der API sind lokale Zeiten.
- Datenquelle ist `https://www.basketball-bund.net/rest/club/id/{clubId}/actualmatches?justHome=false&rangeDays=7`. Sie liefert die vergangenen und die kommenden 7 Tage, Heim- und Auswärtsspiele. Diese Felder sind relevant und wurden am 22.11.2026 geprüft:
  - `kickoffDate` (`yyyy-MM-dd`) und `kickoffTime` (`HH:mm`)
  - `homeTeam` und `guestTeam` mit `teamname`, `teamnameSmall`, `teamPermanentId` und `clubId`. Bad Laer ist das Team mit `clubId == 621`.
  - `result`: `"heim:gast"`, zum Beispiel `"57:64"`, oder `null`, solange kein Ergebnis eingetragen ist
  - `abgesagt` (bool) und `verzicht` (bool)
  - `ligaData.akName`: `"Senioren"` steht für die Herren, `"U12"`, `"U14"` usw. für die Jugend.
  - `ligaData.skName`: der Name der Spielklasse, zum Beispiel „Bezirksliga“
- Den Spielort liefert `rest/match/id/{matchId}/matchInfo` → `spielfeld.ort` (wird heute schon genutzt).
- Das Gegnerlogo kommt von `media/team/{teamPermanentId}/logo`. Eine Antwort unter 2000 Byte bedeutet: kein Logo (bisherige Logik).
- Die Vereinsfarben sind Blau (`#0B4EA2`) und Weiß. Gelb oder andere Fremdakzente werden nicht verwendet. Als zweiter Akzent ist Hellblau erlaubt (`#6FA8F0` / `#A9CBF7`).
- Das Story-Format ist 1080 × 1920 px.

## Lösungswege (Approaches)

Zur Wahl stand, wie die Bilder gezeichnet werden:

1. **SkiaSharp beibehalten, neu strukturiert.** Es gibt eine Vorlagen-Klasse pro Story-Typ und gemeinsame Zeichenbausteine. Theme-Werte liegen zentral, die Schrift kommt als Datei ins Projekt. Das bleibt schlank und passt in die Cloud Function. Der Nachteil: Das Layout steht im Code.
2. **HTML-Vorlagen und Headless-Chromium (Playwright).** Das Design ließe sich sehr leicht ändern. Dafür braucht es etwa 300 MB Chromium und 1–2 GB RAM, praktisch also einen Umzug auf Cloud Run. Der Start ist langsamer, und es gibt mehr Fehlerquellen.
3. **SVG und Svg.Skia.** Diese Variante scheidet aus. Es gibt keinen Textumbruch und kein Kürzen, und das ist für die Gegnernamen nötig.

**Gewählt wurde Variante 1** (Entscheidung des Auftraggebers). Der Grund: Es zählt ein zuverlässiger und günstiger Automatikbetrieb. Das Design ist festgelegt und ändert sich danach selten.

## Abläufe (entschieden)

### Wochenvorschau (`preview`)

- Cloud Scheduler startet sie **montags um 08:00 Europe/Berlin**, einmal und **ohne automatische Wiederholung**.
- Zeitraum ist Montag (heute) bis Sonntag der laufenden Woche.
- Ausgeschlossen werden Spiele mit `abgesagt == true` oder `verzicht == true`.
- Die Spiele werden in vier Gruppen eingeteilt. Gruppen ohne Spiele entfallen.
  - Herren Heim: eine Story pro Spiel
  - Herren Auswärts: eine Story pro Spiel
  - Jugend Heim: eine Liste, sortiert nach Anpfiff, aufgeteilt auf mehrere Stories, wenn sie nicht auf eine passt
  - Jugend Auswärts: wie Jugend Heim
- Alle Vorschau-Bilder der Woche gehen in **einem** Upload-Aufruf hoch, alles oder nichts. Danach wird die Kalenderwoche als gepostet vermerkt (`previewWeek`, ISO-Woche, z. B. `"2026-W48"`). Ist die aktuelle Woche dort schon vermerkt, postet ein erneuter Lauf nichts.
- Schlägt ein Lauf fehl, muss man ihn von Hand neu anstoßen. Die Funktion antwortet dann mit HTTP 500, damit der Fehler im Log sichtbar ist. Dank `previewWeek` ist ein Neustart gefahrlos.

### Tagesergebnisse (`results`)

- Cloud Scheduler startet den Lauf **stündlich von 12:00 bis 23:00 Europe/Berlin**, jeden Tag. Gibt es an einem Tag keine Spiele, endet der Lauf sofort, das kostet nichts.
- Berücksichtigt werden alle Spiele mit Anpfiff am heutigen Tag, Heim und Auswärts, ohne abgesagte Spiele und ohne Verzicht.
- **Herren:** Jedes Herrenspiel mit Ergebnis, dessen `matchId` noch nicht als gepostet vermerkt ist, wird sofort als eigene Story gepostet und danach vermerkt. Das geschieht unabhängig von der Jugend.
- **Jugend:** Die Jugend wird als Sammel-Story gepostet, sobald **alle** heutigen Jugendspiele ein Ergebnis haben. Spätestens im **23-Uhr-Lauf** wird gepostet, was ein Ergebnis hat. Spiele ohne Ergebnis entfallen dann ersatzlos. Haben im 23-Uhr-Lauf gar keine Jugendspiele ein Ergebnis, wird nichts gepostet. Nach dem Posten wird `jugendPosted = true` gesetzt.
- Was nach dem Tages-Post noch eingetragen wird, wird nicht mehr gepostet. Das ist bewusst so.

### Merken, was gepostet wurde

In einem Cloud-Storage-Bucket liegt die Datei `posted.json`. Lokal ist es eine Datei im Arbeitsordner.

```json
{ "previewWeek": "2026-W48", "date": "2026-11-21", "herrenMatchIds": [2871157], "jugendPosted": true }
```

- Steht in der Datei ein anderes Datum als heute (Europe/Berlin), gelten `herrenMatchIds` und `jugendPosted` als leer. `previewWeek` bleibt davon unberührt und wird nur überschrieben. Die Datei wächst deshalb nie.
- Der Zustand wird **nach jedem erfolgreichen Upload** geschrieben, nicht erst am Ende des Laufs. Schlägt ein Upload fehl, wird nichts vermerkt, und der nächste stündliche Lauf versucht es erneut.

### Weitere Regeln (teils vom Auftraggeber bestätigt, teils Entscheidungen des Umsetzungs-Teams)

- **Erst alles zeichnen, dann hochladen** (bestätigt). Ein Lauf erzeugt zuerst alle Bilder. Erst wenn das ohne Fehler geklappt hat, wird hochgeladen.
- **Ein Upload-Aufruf pro Beitrag.** Mehrteilige Jugend-Beiträge gehen in einem einzigen Aufruf an upload-post.com, als mehrere `photos[]`. Damit gilt alles oder nichts, und `jugendPosted` bleibt ein einzelner Wert. Ein Beitrag gilt als gepostet, wenn upload-post.com den Aufruf erfolgreich annimmt. Ob die Story danach auf Instagram erscheint, wird nicht geprüft. Kann upload-post.com mehrere Bilder bei Stories nicht in einem Aufruf annehmen, merkt sich der Zustand stattdessen die bereits hochgeladenen Teile.
- **Platzhalterzeiten:** Spiele mit Anpfiff vor 07:00 werden ignoriert, wie bisher (bestätigt). Grund: Es sind Spiele, für die noch keine Zeit gesetzt ist oder die möglicherweise ausfallen.
- **Herren:** Es gibt genau eine Herrenmannschaft (`akName == "Senioren"`). Die Beschriftung „HERREN“ ist eindeutig (bestätigt).
- **Vereinsinterne Spiele** gibt es laut Auftraggeber nicht. Tritt doch eins auf (beide `clubId == 621`), wird es mit einer Warnung im Log übersprungen.
- **Unentschieden** gibt es im Basketball nicht. Ein Sieg liegt vor, wenn Bad Laer mehr Punkte hat, sonst ist es eine Niederlage.
- **Ausfall um 23 Uhr:** Scheitert der 23-Uhr-Lauf, entfallen die Jugend-Ergebnisse dieses Tages. Das ist akzeptiert (bestätigt), es gibt keinen Nachhol-Lauf.

## Aussehen (entschieden)

Die Entwürfe liegen unter `.superpowers/brainstorm/…/content/`: `herren-b-v3.html`, `results-v1.html` und `style-v2.html` (Jugend). Sie dienen als verbindliche Vorlage für Anordnung und Wirkung. Die genauen Pixelwerte darf der Umsetzer anpassen, es gilt aber: **nichts überlappt**.

### Herren (Stil „Matchday“): Vorschau und Ergebnis

Es gibt (noch) nicht genug Herren-Fotos. Deshalb hat der Auftraggeber am 22.11.2026 die Variante B „Duell im Fokus“ aus `herren-platzhalter.html` gewählt: Statt eines Fotos zeigt der obere Bereich den Spielfeld-Hintergrund mit beiden Logos.

- Der obere Bereich reicht bis etwa 55 % der Höhe und endet mit einem Schrägschnitt, der nach rechts ansteigt. Entlang der Schräge verläuft eine weiße Kante, darunter eine dünne hellblaue Linie. Unterhalb ist die Fläche vollflächig Vereinsblau.
- Im oberen Bereich liegt der Spielfeld-Hintergrund der Jugend-Stories (Verlauf und Feldlinien, siehe unten), auf die Schräge zugeschnitten. Darauf stehen sich **beide Logos groß gegenüber: links die Heimmannschaft, rechts der Gast**, jeweils auf einem weißen Kreis (Entwurf: Mitte bei y = 150, Radius 47,5 von 300 × 533). Bad Laer zeigt das Vereinslogo. Hat der Gegner kein Logo, steht ersatzweise sein `teamnameSmall` im weißen Kreis (erst verkleinert, dann mit „…“ gekürzt). Zwischen den Logos steht ein weißes „VS“.
- Oben links sitzt ein Etikett, **immer weiß hinterlegt mit blauer Schrift**, damit es sich vom Spielfeld abhebt:
  - in der Vorschau „HEIMSPIEL“ oder „AUSWÄRTSSPIEL“
  - im Ergebnis „ENDSTAND“
- **Vorschau:** Unter der Schräge steht groß „GAME DAY“, zweizeilig. Der Text bleibt englisch, so hat es der Auftraggeber entschieden. Darunter folgt die Zeile „SAMSTAG, 28.11. · 19:30 UHR“, für Heim- und Auswärtsspiele gleich; der Ort steht nur in der Fußzeile.
- **Ergebnis:** An der Stelle von „GAME DAY“ steht groß der Spielstand im Format Heim : Gast. Die Punkte der unterlegenen Mannschaft sind abgeschwächt. Bei einem Sieg von Bad Laer steht darunter „SIEG!“, bei einer Niederlage gibt es keine Wertungszeile. Danach folgt die Zeile mit Datum und Spielklasse.
- **Fußzeile:** Unter einer feinen Trennlinie steht nur Text, ohne Logos (die stehen oben), zentriert in zwei Zeilen:
  - groß „{HEIM} VS {GAST}“; Bad Laer erscheint als „HERREN“. Ist die Zeile zu lang, wird sie erst verkleinert und dann nur der Gegnername mit „…“ gekürzt, sodass „VS HERREN“ bzw. „HERREN VS“ lesbar bleibt.
  - darunter klein in Hellblau: in der Vorschau „{Halle} · {SPIELKLASSE}“, wobei die Halle bei Heimspielen „SPORTHALLE BAD LAER“ ist und bei Auswärtsspielen der Ort (fehlt der Ort, entfällt dieser Teil); im Ergebnis nur „{SPIELKLASSE}“.
- Herren-Stories verwenden keine Fotos mehr; jede Story sieht bei gleichen Daten gleich aus (kein Zufall). Die Fotos in `Images/Herren/` bleiben im Projekt, falls später wieder auf Fotos umgestellt wird.

### Jugend (Stil „Infokarte“): Vorschau und Ergebnis

- Der Hintergrund ist kein Foto, sondern eine rein grafische, im Code gezeichnete Fläche (Variante A „Spielfeld“ aus `jugend-hintergrund.html`, Entscheidung des Auftraggebers vom 22.11.2026): ein radialer Verlauf von `#1663C7` in der Mitte (Lichtkegel bei 50 % Breite, 38 % Höhe) nach `#062A5E` am Rand, darüber dezent (13 % Deckkraft) die weißen Linien eines Basketballfelds (Außenlinie, Mittellinie, Mittelkreis, Zonen, Freiwurfkreise und Dreierlinien). Eine Abdunklung gibt es nicht mehr; die weißen Karten haben einen leichten Schatten.
- Kopfbereich, zentriert:
  - kleine Zeile „SV BAD LAER · JUGEND“
  - groß „HEIMSPIELE“, „AUSWÄRTSSPIELE“ oder „ERGEBNISSE“
  - darunter der Zeitraum („Diese Woche · 23.–29.11.“) oder das Datum („Samstag, 28.11.“)
- Darunter steht eine Liste weißer, abgerundeter Karten, eine pro Spiel:
  - links ein blaues Quadrat mit der Altersklasse („U12“)
  - in der Mitte der Gegner („vs X“ bei Heimspielen, „@ X“ bei Auswärtsspielen; zu lange Namen werden mit „…“ gekürzt) und darunter eine Infozeile
  - In der **Vorschau** lautet die Infozeile „Sa 28.11. · 10:00 Uhr“, ergänzt um „· in {Ort}“, wenn der Ort nicht Bad Laer ist. Rechts steht das Sponsorlogo mit „unterstützt von“, falls die Altersklasse einen Sponsor hat.
  - Im **Ergebnis** lautet die Infozeile „Heim“ oder „Auswärts“. Rechts steht der Spielstand **aus Sicht von Bad Laer**, also mit den eigenen Punkten vorn, und darunter „SIEG“ oder „NIEDERLAGE“. Siege haben einen blauen linken Rand, Niederlagen einen grauen und abgeschwächten Spielstand. Hat die Altersklasse einen Sponsor, steht unter „Heim“/„Auswärts“ eine kleine, dezente Zeile „unterstützt von [Logo]“ (kleine graue Schrift, Logo etwa 1,6-mal so hoch wie die Schriftgröße, linksbündig mit dem Gegnernamen); Gegnername und Spielstand behalten ihre volle Breite (Variante B aus `sponsor-results.html`, Entscheidung des Auftraggebers vom 22.11.2026).
- Die Fußzeile ist ein kurzer Slogan: in der Vorschau „KOMMT VORBEI & FEUERT UNS AN!“, im Ergebnis „STARK GESPIELT, TEAMS!“.
- **Aufteilung:** Passt die Liste nicht auf ein Bild (Richtwert etwa 6 Karten), wird sie auf mehrere Bilder verteilt, und zwar gleichmäßig, also 7 Spiele als 4 + 3 und nicht als 6 + 1. Die Reihenfolge bleibt chronologisch.
- Der Spielfeld-Hintergrund ist bei jeder Jugend-Story gleich (kein Zufall); `Images/Jugend/` wird nicht mehr verwendet.

### Gemeinsam für beide Stile

- Schriften: Anton für Überschriften, Zahlen und Namen, Inter für kleine Info-Zeilen. Beide werden **als TTF-Dateien ins Projekt aufgenommen** und explizit geladen, statt sich auf installierte Systemschriften zu verlassen.
- Datums- und Wochentagsangaben sind deutsch (Kultur `de-DE`).
- Gegnernamen werden bereinigt: „(ak)“ wird entfernt (ohne Rücksicht auf Groß- und Kleinschreibung) und Leerzeichen gekürzt (bisherige Logik).
- Die Ausgabe ist JPEG in hoher Qualität im Format 1080 × 1920. Beide Stile verwenden den im Code gezeichneten Spielfeld-Hintergrund, keine Fotos.

## Codeaufbau (Rahmen)

Die folgende Aufteilung ist der Vorschlag. Namen und Details legt der Umsetzer fest.

- **Datenzugriff:** `BasketballBundClient`. Er lädt die Spiele, die Spielinfos (Ort) und die Logos. Er gibt API-DTOs zurück und hat keine Logik.
- **Fachmodell:** ein unveränderliches `Game` mit Teams, Anpfiff (lokale Zeit), Altersklasse, Spielklasse, Ort, Ergebnis (optional), `IsHome`, `IsHerren`, Bad-Laer-Punkten und Gegnerpunkten sowie `IsWin`. Die Umrechnung von den DTOs geschieht an genau einer Stelle.
- **Auswahl:** reine Funktionen ohne Ein- und Ausgabe, also leicht testbar. Für die Vorschau gruppieren sie die Spiele zu Beiträgen. Für die Ergebnisse entscheiden sie anhand von Spielen, Uhrzeit und Zustand, was jetzt gepostet wird.
- **Zeichnen:** je eine Vorlage pro Story-Typ (`HerrenPreviewStory`, `HerrenResultStory`, `JugendPreviewStory`, `JugendResultStory`). Dazu kommen gemeinsame Bausteine (Foto zuschneiden, Schrägschnitt, Karte, Text mit Kürzen, Logo oder Kürzel-Kreis) und ein zentrales `Theme` mit Farben, Schriften und Abständen.
- **Ressourcen:** `AssetStore` für Fotos, Vereinslogo, Sponsoren und Schriften. Die Zuordnung von Sponsor zu Altersklasse liegt in der Konfiguration statt im Code.
- **Veröffentlichen:** `IStoryPublisher` mit zwei Umsetzungen: `UploadPostPublisher` (upload-post.com) und `LocalFolderPublisher`, der nur Dateien schreibt.
- **Zustand:** `IPostedStateStore` mit zwei Umsetzungen: `GcsPostedStateStore` und `FilePostedStateStore`.
- **Uhr:** `IClock` liefert „jetzt“ in Europe/Berlin, damit die Logik testbar bleibt.
- **Einstieg:**
  - Cloud Function: Der Lauf wird über einen Query-Parameter gewählt, `?job=preview` oder `?job=results`. Bei Erfolg antwortet sie mit 200, bei Fehlern mit 500.
  - Konsole: `preview` und `results` mit den Optionen `--dry-run` (lokaler Ordner, kein Upload, kein Zustand), `--sample` (Beispieldaten aller vier Typen mit langen Namen, ohne Logo, mit vielen Jugendspielen) und `--now <Zeitpunkt>`, um eine Uhrzeit vorzugeben.
- Die statischen Klassen fallen weg. Die Abhängigkeiten werden im Einstiegspunkt zusammengesetzt, ohne DI-Framework, falls es nicht nötig ist.

## Konfiguration

Die Werte kommen aus Umgebungsvariablen. Lokal ist zusätzlich eine `appsettings.json` erlaubt.

- `CLUB_ID` (621)
- `HOME_PLACE` („Bad Laer“): Mit diesem Ort wird ein Spielort verglichen. Er wird außerdem als Hallenname „Sporthalle Bad Laer“ verwendet.
- `UPLOAD_POST_API_KEY` und `UPLOAD_POST_USER` („Dachse“)
- `STATE_BUCKET` (Cloud Storage)
- die Sponsor-Zuordnung zu den Altersklassen (heute U12 → Böhnke, U14 → Mann, U16 → Stockmeyer, U18 → Mittelstedt)

## Bekannte Fehler im Bestand, die mit dem Umbau verschwinden

- Alle Herrenbilder werden nach `output.png` geschrieben und überschreiben sich gegenseitig.
- Der Upload ist durch `return;` abgeschaltet. In `form.Add` sind Name und Inhalt vertauscht, und es werden keine Dateiinhalte gesendet.
- Es wird nach `AppContext.BaseDirectory` geschrieben, und das ist in der Cloud Function schreibgeschützt.
- Der Jugend-Lauf lädt das Herrenfoto (`LoadSenioren`). Außerdem schließen `Content Remove`-Einträge in der csproj fast alle Fotos von der Ausgabe aus.
- `DateTime.UtcNow` wird mit der lokalen Anstoßzeit verglichen.
- Abgesagte Spiele (`abgesagt`) werden heute nicht gefiltert.

## Dem Umsetzer überlassen

- die genauen Maße, Schriftgrößen und Abstände nach den Entwürfen, solange nichts überlappt und alles lesbar bleibt
- die genaue Kartenzahl pro Jugend-Bild, bestimmt nach Platz
- die Wahl des Test-Frameworks und welche Teile Unit-Tests bekommen. Pflicht sind Tests für die Auswahl- und Ergebnislogik sowie für die Umrechnung aus Sicht von Bad Laer.
- die Bereitstellungsskripte für Cloud Scheduler, Bucket und Function
