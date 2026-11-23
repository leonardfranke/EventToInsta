# Bereitstellung: Cloud Function und Cloud Scheduler

EventToInsta läuft als Google Cloud Function (2. Generation, Runtime `dotnet8`) in der Region
`europe-west3`. Cloud Scheduler ruft die Function per HTTP auf, `?job=preview` oder `?job=results`.
Der Zustand (`posted.json`) liegt in einem Cloud-Storage-Bucket.

Alle Befehle laufen im Wurzelverzeichnis des Repositorys (dort liegt der Ordner `EventToInsta/`).

## Platzhalter

| Platzhalter        | Bedeutung                                                                                     |
|--------------------|-----------------------------------------------------------------------------------------------|
| `<PROJECT_ID>`     | ID des Google-Cloud-Projekts                                                                  |
| `<PROJECT_NUMBER>` | Projektnummer (`gcloud projects describe <PROJECT_ID> --format='value(projectNumber)'`)       |
| `<BUCKET>`         | Name des Buckets für den Zustand, z. B. `eventtoinsta-state-<PROJECT_ID>`                    |
| `<SECRET_NAME>`    | Name des Secrets mit dem API-Key von upload-post.com, z. B. `upload-post-api-key`            |
| `<RUNTIME_SA>`     | Service-Account, unter dem die Function läuft (siehe unten)                                   |
| `<INVOKER_SA>`     | Service-Account, mit dem Cloud Scheduler die Function aufruft                                 |
| `<FUNCTION_URL>`   | URL der Function, ohne Query (siehe Schritt 5)                                                |

Empfohlen ist ein eigener Laufzeit-Service-Account nur für diese Function, der genau zwei Rechte bekommt:
`roles/secretmanager.secretAccessor` auf dem Secret (Schritt 2) und `roles/storage.objectAdmin` auf dem
Bucket (Schritt 4). Ohne `--service-account` liefe die Function unter dem Compute-Standardkonto
(`<PROJECT_NUMBER>-compute@developer.gserviceaccount.com`), das in vielen Projekten weitreichende Rechte hat.

```sh
gcloud iam service-accounts create eventtoinsta-runtime --display-name="EventToInsta Function"
# <RUNTIME_SA> = eventtoinsta-runtime@<PROJECT_ID>.iam.gserviceaccount.com
```

Nach dem Deploy lässt sich prüfen, unter welchem Konto die Function läuft:

```sh
gcloud functions describe eventtoinsta --gen2 --region=europe-west3 \
  --format='value(serviceConfig.serviceAccountEmail)'
```

Vorab einmalig:

```sh
gcloud config set project <PROJECT_ID>
gcloud services enable cloudfunctions.googleapis.com run.googleapis.com cloudbuild.googleapis.com \
  artifactregistry.googleapis.com cloudscheduler.googleapis.com secretmanager.googleapis.com storage.googleapis.com
```

## Umgebungsvariablen

| Variable              | Wert in der Cloud                     | Hinweis                                                    |
|-----------------------|---------------------------------------|------------------------------------------------------------|
| `STATE_BUCKET`        | `<BUCKET>`                            | Pflicht. Fehlt er, antwortet die Function mit 500.         |
| `UPLOAD_POST_USER`    | `Dachse`                              | Profil bei upload-post.com. Für Tests ein Test-Profil.     |
| `UPLOAD_POST_API_KEY` | aus Secret Manager (`<SECRET_NAME>`)  | Nie als Klartext in `--set-env-vars`.                      |
| `CLUB_ID`, `HOME_PLACE`, `SPONSORS` | nicht gesetzt            | Defaults: `621`, „Bad Laer“, `U12=Böhnke.png;U14=Mann.png;U16=Stockmeyer.webp;U18=Mittelstedt.png` |

> **Umbenennung:** Die alte Umgebungsvariable `upload_posts_api_key` ersetzt jetzt `UPLOAD_POST_API_KEY`.
> Eine bestehende Function oder ein bestehendes Secret-Mapping mit dem alten Namen wird nicht mehr gelesen.

## 1. Bucket anlegen

```sh
gcloud storage buckets create gs://<BUCKET> --location=europe-west3
```

## 2. API-Key als Secret ablegen

```sh
# Key nicht in die Shell-History schreiben: z. B. aus einer Datei lesen und die Datei danach löschen.
gcloud secrets create <SECRET_NAME> --replication-policy=automatic --data-file=<DATEI_MIT_KEY>
# oder interaktiv:  printf '%s' "$KEY" | gcloud secrets create <SECRET_NAME> --replication-policy=automatic --data-file=-
```

Der Service-Account der Function braucht Lesezugriff auf das Secret (vor dem Deploy, sonst startet die
Revision nicht):

```sh
gcloud secrets add-iam-policy-binding <SECRET_NAME> \
  --member=serviceAccount:<RUNTIME_SA> --role=roles/secretmanager.secretAccessor
```

Neuen Key hinterlegen: `gcloud secrets versions add <SECRET_NAME> --data-file=<DATEI_MIT_KEY>`.
Wegen `:latest` greift er beim nächsten Kaltstart, sofort mit einem erneuten Deploy.

## 3. Function deployen

```sh
gcloud functions deploy eventtoinsta --gen2 --runtime=dotnet8 --region=europe-west3 \
  --source=EventToInsta --entry-point=EventToInsta.CloudFunction --trigger-http --no-allow-unauthenticated \
  --memory=512Mi --timeout=1800s --service-account=<RUNTIME_SA> \
  --set-env-vars=STATE_BUCKET=<BUCKET>,UPLOAD_POST_USER=Dachse \
  --set-secrets=UPLOAD_POST_API_KEY=<SECRET_NAME>:latest
```

Zum Build: Der Buildpack führt `dotnet publish` im Ordner `EventToInsta/` aus. `Images/**` und `Assets/**`
(Schriften Anton und Inter) werden dabei mit veröffentlicht. `EventToInsta/.gcloudignore` hält lokale
`bin/` und `obj/` aus dem Upload heraus. `Program.Main` erkennt die Function an der Variablen
`FUNCTION_TARGET`, die die Plattform setzt, und startet dann den Functions-Host statt der CLI.

### Deploy per GitHub Action

Statt Schritt 3 von Hand: `.github/workflows/deploy.yml` (GitHub → Actions → „Deploy Cloud Function“ →
„Run workflow“). Der Workflow lässt die Tests laufen und führt dann denselben `deploy`-Befehl aus. Das
Profil bei upload-post.com ist ein Eingabefeld (Standard `Dachse`).

Den API-Key pflegt man nur in GitHub, als Repo-Secret `UPLOAD_POST_API_KEY`. Vor dem Deploy schreibt
der Workflow den Key als neue Version in `upload-post-api-key` (Secret Manager), aber nur, wenn er sich
geändert hat. Die Function liest den Key wie oben über `--set-secrets`, nie im Klartext. Neuer Key:
GitHub-Secret ändern und den Workflow neu starten.

Die Anmeldung läuft schlüssellos über Workload Identity Federation (Pool `github`, Provider
`github-eventtoinsta`, nur für das Repo `leonardfranke/EventToInsta`). Dabei übernimmt der Workflow den
Service-Account `eventtoinsta-deployer`. Dessen Rechte: `roles/cloudfunctions.developer` im Projekt und
`roles/iam.serviceAccountUser` auf `eventtoinsta-runtime` und auf dem Compute-Standardkonto (Build) sowie
`roles/secretmanager.secretVersionAdder` und `roles/secretmanager.secretAccessor` auf dem Secret. Das
Compute-Standardkonto hat `roles/cloudbuild.builds.builder`.

## 4. Bucket-Rechte für die Function

```sh
gcloud storage buckets add-iam-policy-binding gs://<BUCKET> \
  --member=serviceAccount:<RUNTIME_SA> --role=roles/storage.objectAdmin
```

## 5. Invoker-Service-Account und Aufrufrecht

```sh
gcloud iam service-accounts create eventtoinsta-invoker --display-name="EventToInsta Scheduler"
# <INVOKER_SA> = eventtoinsta-invoker@<PROJECT_ID>.iam.gserviceaccount.com

# 2. Generation: Aufrufrecht ist roles/run.invoker auf dem zugrunde liegenden Cloud-Run-Dienst.
gcloud functions add-invoker-policy-binding eventtoinsta --region=europe-west3 \
  --member=serviceAccount:<INVOKER_SA>
# gleichwertig:
# gcloud run services add-iam-policy-binding eventtoinsta --region=europe-west3 \
#   --member=serviceAccount:<INVOKER_SA> --role=roles/run.invoker

# URL der Function (ohne Query) für die Scheduler-Jobs:
gcloud functions describe eventtoinsta --gen2 --region=europe-west3 --format='value(serviceConfig.uri)'
```

## 6. Scheduler-Jobs

Beide Jobs ohne Wiederholungen (`--max-retry-attempts=0`). Doppelte Posts verhindern Zustand und
`Idempotency-Key`. Was nach einem Fehlschlag passiert, hängt vom Job ab:

- **Ergebnisse:** Ein fehlgeschlagener Beitrag wird beim nächsten stündlichen Lauf nachgeholt.
- **Vorschau:** Läuft nur montags um 08:00. Ein Fehlschlag wird **nicht** automatisch nachgeholt, sondern
  muss von Hand neu ausgelöst werden (`gcloud scheduler jobs run eventtoinsta-preview --location=europe-west3`).
  Das ist gefahrlos: Ist die Vorschau der Woche schon gepostet (`previewWeek` in `posted.json`), wird
  sie übersprungen.
- **Ausnahme:** Steht im Log `VERÖFFENTLICHT, Zustand NICHT gespeichert – <key> – nicht erneut auslösen`,
  ist der Beitrag online, aber `posted.json` wurde nicht geschrieben. Dann **nicht** erneut auslösen
  (Ergebnisse: Job pausieren), sondern zuerst den Bucket-Fehler beheben und `posted.json` von Hand
  nachtragen (`previewWeek` bzw. `date`, `herrenMatchIds`, `jugendPosted` für den Tag).

Das OIDC-Token braucht als Audience die URL **ohne** Query. Die Uploads laufen nacheinander mit je bis
zu 120 s Timeout; deshalb Function-Timeout `1800s` (2. Generation erlaubt bei HTTP bis 3600 s) und
`--attempt-deadline=1800s` (Höchstwert bei Cloud Scheduler). Mit dem Standard von 180 s würde der
Scheduler einen langen Lauf abbrechen.

```sh
gcloud scheduler jobs create http eventtoinsta-preview --location=europe-west3 \
  --schedule="0 8 * * 1" --time-zone="Europe/Berlin" --max-retry-attempts=0 --attempt-deadline=1800s \
  --uri="<FUNCTION_URL>?job=preview" --http-method=GET \
  --oidc-service-account-email=<INVOKER_SA> --oidc-token-audience="<FUNCTION_URL>"

gcloud scheduler jobs create http eventtoinsta-results --location=europe-west3 \
  --schedule="0 12-23 * * *" --time-zone="Europe/Berlin" --max-retry-attempts=0 --attempt-deadline=1800s \
  --uri="<FUNCTION_URL>?job=results" --http-method=GET \
  --oidc-service-account-email=<INVOKER_SA> --oidc-token-audience="<FUNCTION_URL>"
```

- `eventtoinsta-preview`: montags 08:00 Uhr, Vorschau der Woche.
- `eventtoinsta-results`: täglich stündlich 12 bis 23 Uhr, neue Ergebnisse.

Wer zuerst ohne Posts deployen will, legt die Jobs an und pausiert sie sofort:
`gcloud scheduler jobs pause eventtoinsta-preview --location=europe-west3` (ebenso `eventtoinsta-results`),
später `gcloud scheduler jobs resume …`.

## 7. Manuell auslösen und Logs

```sh
gcloud scheduler jobs run eventtoinsta-preview --location=europe-west3
gcloud functions logs read eventtoinsta --gen2 --region=europe-west3 --limit=50
gcloud storage cat gs://<BUCKET>/posted.json
```

> **Achtung:** Jeder Aufruf mit `?job=preview` oder `?job=results` postet echt auf Instagram. Einen
> Testlauf (`--dry-run`) gibt es nur in der CLI, nie in der Function.

## Abnahmeprüfung upload-post.com (vor dem Aktivieren des Schedulers)

Der Schutz vor Doppelposts bei Wiederholungen beruht auf dem `Idempotency-Key` (je Bild
`<postKey>-<Bildname>`, z. B. `jugend-result-2026-11-22-jugend-result-2026-11-22-1-1a2b3c4d`). Zwei
Eigenschaften von upload-post.com sind dafür **nicht dokumentiert** und müssen vor dem Fortsetzen der
Scheduler-Jobs geklärt sein (Support fragen oder mit dem Test-Profil ausprobieren):

1. **Aufbewahrungsdauer:** Wie lange merkt sich upload-post.com einen `Idempotency-Key`? Er muss
   mindestens bis zum nächsten Wiederholungslauf halten (Ergebnisse: Stunden, Vorschau: bis zum
   manuellen Neuauslösen, also unter Umständen Tage).
2. **Fehlgeschlagener Job:** Was liefert ein erneuter Aufruf mit dem Key eines Jobs, der bei
   upload-post.com **fehlgeschlagen** ist? Erwartet wird ein neuer Versuch. Liefert er stattdessen
   die alte Fehlerantwort, wird das Bild nie gepostet; liefert er `success: true` ohne neuen Upload,
   wird es fälschlich als gepostet verbucht.

Sind die Antworten ungünstig (kurze Aufbewahrung, Fehler-Jobs werden nicht neu versucht), gilt der
Ausweichweg: angenommene Teile eines Beitrags einzeln in `posted.json` speichern und bei einer
Wiederholung nur die fehlenden Bilder hochladen, statt sich auf den Key zu verlassen. Das ist eine
Code-Änderung und vor dem Aktivieren der Jobs umzusetzen.

## Erster sicherer Test

1. Deployen, Scheduler-Jobs anlegen und sofort pausieren (siehe oben). Die Abnahmeprüfung
   upload-post.com (oben) muss erledigt sein, bevor die Jobs fortgesetzt werden.
2. Möglichst zuerst gegen ein Test-Profil von upload-post.com: den `deploy`-Befehl aus Schritt 3 mit
   `UPLOAD_POST_USER=<TEST_PROFIL>` statt `Dachse` ausführen.
3. Erst wenn der Auftraggeber bereit ist und zuschaut: `gcloud scheduler jobs run eventtoinsta-preview --location=europe-west3`.
4. Logs und `posted.json` prüfen (Smoke-Check unten). Ein zweiter Lauf darf nichts doppelt posten
   („bereits gepostet, übersprungen“ im Log).
5. `UPLOAD_POST_USER=Dachse` wieder setzen und die Jobs fortsetzen (`resume`).

## Smoke-Check nach dem ersten Lauf (Linux, Zeitzone, ICU)

Die Function rechnet in der Zeitzone `Europe/Berlin` und formatiert Texte mit der Kultur `de-DE`.
Beides braucht unter Linux die Zeitzonendaten (tzdata) und ICU. Das Laufzeit-Image von `dotnet8`
bringt beides mit; geprüft wird es trotzdem einmal in den Logs eines manuellen Laufs:

- **Erfolg:** Zeilen wie `Veröffentlicht: preview-2026-W48` oder `Keine Spiele für die Vorschau, nichts zu posten.`,
  HTTP-Antwort 200 `ok`. Die Kalenderwoche im Schlüssel muss zur Berliner Woche passen.
- **Zeitzone fehlt:** `Job … fehlgeschlagen: Zeitzone Europe/Berlin nicht verfügbar. Auf dem System fehlen
  Zeitzonendaten (tzdata/ICU): …` (HTTP 500).
- **ICU fehlt:** Die Function startet gar nicht, im Log steht `Couldn't find a valid ICU package installed on the system`.
  Nicht mit `InvariantGlobalization` umgehen, sonst gibt es keine deutschen Texte (`de-DE`).
- **Grafik:** Das Projekt bindet die Linux-Natives direkt ein, passend zu den verwalteten Paketen:
  `SkiaSharp.NativeAssets.Linux.NoDependencies` 3.119.1 (zu `SkiaSharp` 3.119.1) und
  `HarfBuzzSharp.NativeAssets.Linux` 7.3.0.1 (zu `HarfBuzzSharp` 7.3.0.1). Die Variante `NoDependencies`
  braucht kein fontconfig im Laufzeit-Image (Schriften werden ohnehin explizit aus `Assets/Fonts` geladen).
  Das von `Topten.RichTextKit` transitiv gezogene `SkiaSharp.NativeAssets.Linux` (2.88.7) ist ausgeschlossen.
  Fehler wie `DllNotFoundException`, `EntryPointNotFoundException` oder „incompatible native library“ zu
  `libSkiaSharp`/`libHarfBuzzSharp` bedeuten, dass wieder ein unpassendes Native-Paket im Publish gelandet ist.
  Prüfen: `dotnet publish EventToInsta -c Release -r linux-x64 --self-contained false -o publish-linux`, dann
  in `EventToInsta/obj/project.assets.json` die Versionen von `*.NativeAssets.Linux*` mit `SkiaSharp` und
  `HarfBuzzSharp` vergleichen. Beim Anheben von SkiaSharp oder RichTextKit die Native-Versionen mitziehen.
- Auf den Stories: deutsche Wochentage und Monatsnamen, Anpfiffzeiten in Berliner Ortszeit,
  Schriften Anton und Inter (keine Ersatzschrift).

## Lokal als Function starten (optional)

```sh
dotnet publish EventToInsta -c Release -o publish
FUNCTION_TARGET=EventToInsta.CloudFunction PORT=8080 dotnet publish/EventToInsta.dll
curl "http://localhost:8080/?job="   # 400: fehlender Job
```

Lokal nie mit `?job=preview|results` und echten upload-post-Zugangsdaten aufrufen. Ohne
`STATE_BUCKET` antwortet die Function mit 500, ohne etwas zu laden.
