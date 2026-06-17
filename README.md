# Raccolta di dati sintetici e annotazioni automatiche in VR

Progetto per la generazione di dataset sintetici tramite **Unity Perception Package**, con randomizzatori personalizzati, metriche custom e script Python di post-processing/verifica.


## Indice

- [Randomizer Unity](#randomizer-unity)
  - [CameraRandomizer](#cameraRandomizer)
  - [CharacterRandomizer](#characterrandomizer)
  - [LightingRandomizer](#lightingrandomizer)
  - [PostProcessingRandomizer e ViolenceRandomizer](#postprocessingrandomizer-e-violencerandomizer)
- [Labeler](#labeler)
  - [CustomMetricsLabeler](#custommetricslabeler)
- [Componenti di scena](#componenti-di-scena)
  - [WalkPath](#walkpath)
- [Formato di output del dataset](#formato-di-output-del-dataset)
  - [captures.json](#capturesjson)
  - [metrics.json](#metricsjson)
- [Script Python](#script-python)
  - [classify_violence.py](#classify_violencepy)
  - [visualize_annotations.py](#visualize_annotationspy)

---

## Randomizer Unity

I randomizer estendono `Randomizer` del Perception Package e operano in `OnIterationStart()`, prima del rendering di ogni frame. Espongono campi statici letti dal `CustomMetricsLabeler` per scrivere metriche nel dataset.

### CameraRandomizer

Simula telecamere di sorveglianza distribuite nella scena. Ad ogni iterazione seleziona casualmente un punto di ripresa (`CameraPosition`) e applica piccole variazioni di posizione, rotazione e FOV.

- **Struttura `CameraPosition`**: `positionName`, `basePosition` (Transform), `maxPositionOffset` (default 0.3 m, asse Y al 50%), `maxRotationOffset` (default 5°, assi X/Y), `fovMin`/`fovMax` (default 50–80°).
- **Campi statici**: `CurrentCameraName`, `CurrentCameraFov`.
- **Metriche prodotte**: `camera_name`, `camera_fov`.
- Richiede il `CustomMetricsLabeler` sulla Perception Camera per essere registrato nel dataset.

### CharacterRandomizer

Gestisce la varietà visiva dei personaggi: per ogni ruolo (slot) sono presenti più varianti sovrapposte nella stessa posizione; ad ogni interazione ne viene mostrata una sola.

- **Struttura `CharacterSlot`**: `slotName` (es. Aggressor, Victim, Bystander), `characterVariants` (array di GameObject).
- **Logica**: per ogni slot seleziona un indice casuale, abilita `Renderer` e `Labeling` solo sulla variante scelta.
- **Nota tecnica**: disabilita `Renderer`/`Labeling` invece di `SetActive(false)`, per non bloccare l'esecuzione di `Update()` (es. script di movimento) sulle varianti non visibili.
- Non gestisce le label dei personaggi (assegnate staticamente o dal `ViolenceRandomizer`); decide solo quale variante è visibile.

### LightingRandomizer

Simula condizioni di illuminazione ambientale (giorno, notte, tramonto) modificando skybox e luce direzionale.

- **Struttura `LightingPreset`**: `presetName`, `skybox` (Material), `lightIntensity`, `lightTemperature` (°K).
- **Comportamento**: assegna lo skybox a `RenderSettings.skybox`, chiama `DynamicGI.UpdateEnvironment()`, applica intensità/temperatura a tutte le luci `Directional` trovate con `FindObjectsOfType<Light>()`, abilitando `useColorTemperature`.
- **Campi statici**: `CurrentLightingValues` (`[intensity, temperature]`), `CurrentPresetName`.
- **Metriche prodotte**: `lighting_values` (float[]), `lighting_preset` (string[]).

### PostProcessingRandomizer e ViolenceRandomizer

- **PostProcessingRandomizer**: espone il campo statico `CurrentPostProcessing` (float[]), letto dal `CustomMetricsLabeler` in `OnBeginRendering()` quando il flag `reportPostProcessing` è attivo, e come metrica `post_processing` con struttura `[contrast, saturation, grain, vignette]`.
- **ViolenceRandomizer**: modifica dinamicamente le label dei personaggi (`aggressor`, `victim`, `standerby`, `weapon`), assegnate altrimenti in modo statico tramite il componente `Labeling` su ciascuna variante. Usa due campi per personaggio, `calmAnchor` e `violentAnchor`: se entrambi sono `null`, non modifica la posizione e coesiste senza conflitti con `WalkPath`; se assegnati, sovrascrive la posizione impostata da `WalkPath` all'inizio di ogni iterazione. Gestisce inoltre lo stato di animazione del personaggio (`Calm` o `Violent`), eseguito in parallelo dall'Animator senza interferire con la rotazione/direzione calcolata da `WalkPath`.

---

## Labeler

### CustomMetricsLabeler

`CameraLabeler` personalizzato che legge i campi statici dei randomizer e li scrive nel dataset come metriche per-frame. È il punto di integrazione tra randomizer e formato di output del Perception Package.

- **Perché un Labeler e non un Randomizer**: l'API 0.11.2 richiede che le metriche per-frame vengano registrate in `OnBeginRendering`, parte della pipeline di rendering; i randomizer operano prima, in `OnIterationStart`, e non hanno accesso diretto al reporting.
- **Proprietà**: `labelerId = "custom_metrics"`, `supportsVisualization = false`.
- **Flag Inspector** (default tutti `true`): `reportLighting`, `reportCamera`, `reportPostProcessing` — permettono di disattivare i gruppi di metriche relativi a randomizer non presenti nella scena, evitando voci vuote.
- **Metodi**:
  - `Setup()`: crea e registra (`DatasetCapture.RegisterMetric`) le `MetricDefinition` per i gruppi attivi.
  - `OnBeginRendering()`: legge i valori correnti dai campi statici e li riporta (`DatasetCapture.ReportMetric`) tramite `GenericMetric`.
- **Metriche prodotte** (con tutti i flag attivi): `lighting_values`, `lighting_preset`, `camera_name`, `camera_fov`, `post_processing`.
- **Nota**: `GenericMetric` accetta solo array omogenei (`float[]` o `string[]`); per questo nome del preset/telecamera e valori numerici sono metriche separate.

---

## Componenti di scena

### WalkPath

`MonoBehaviour` standard (non un Randomizer) che muove un personaggio lungo una sequenza ciclica di waypoint tramite `Update()`.

- **Campi**: `waypoints` (Transform[]), `speed` (default 1.5 unità/s).
- **Comportamento**: `Vector3.MoveTowards` verso il waypoint corrente, `transform.LookAt` con Y fissa (nessuna inclinazione verticale), avanzamento ciclico (`% length`) sotto soglia di 0.2 unità di distanza.
- **Interazione con il Fixed Length Scenario**: `Update()` viene congelato tra le iterazioni; la posizione al momento della cattura dipende dal tempo trascorso dall'inizio dell'iterazione, aumentando la variabilità posizionale.
- **Compatibilità con `ViolenceRandomizer`**: se al personaggio non sono assegnati anchor, coesiste senza conflitti; se assegnati, gli anchor sovrascrivono la posizione di `WalkPath` a inizio iterazione.
- `Apply Root Motion` deve essere disabilitato sull'Animator per evitare conflitti con il movimento calcolato.

---

## Formato di output del dataset

### captures.json

File `captures_XXX.json` (~150 frame per file) con struttura radice `{ version, captures: [...] }`.

Ogni **capture** contiene: `id`, `sequence_id`, `step` (sempre 0, Fixed Length Scenario), `timestamp`, `sensor` (posizione/rotazione/intrinsics camera), `ego`, `filename` immagine RGB, `format` (PNG), `annotations`.

Tre labeler attivi in `annotations`:

| Labeler | Output |
|---|---|
| `BoundingBox2DLabeler` | `label_id`, `label_name`, `instance_id`, `x`, `y`, `width`, `height` |
| `SemanticSegmentationLabeler` | percorso PNG maschera, colore fisso per classe |
| `InstanceSegmentationLabeler` | percorso PNG maschera + mappa `instance_id → colore RGBA` |

`instance_id` è coerente tra bounding box e instance segmentation nello stesso frame. Oggetti parzialmente fuori frame hanno bounding box troncata al bordo (x=0 o y=0).

### metrics.json

File `metrics_XXX.json` con struttura radice `{ version, metrics: [...] }`. Ogni voce ha `capture_id`/`annotation_id` (sempre `null` in questo dataset), `sequence_id` (collega la metrica al frame in captures), `step`, `metric_definition`, `values`.

Per ogni frame vengono prodotte sempre due metriche base:

- `scenario_iteration`: indice progressivo del frame (0…N-1).
- `random-seed`: seed RNG usato per quell'iterazione (riproducibilità completa di camera, luce, ecc.).

Con 500 frame totali: 500 voci in captures, 1000 in metrics (2 per frame), suddivisi su più file chunk.

---

## Script Python

### classify_violence.py

Post-processing che classifica ogni frame come violento/non violento leggendo le bounding box già finalizzate, e scrive il risultato come metrica `scene_violence` in copie dei file metrics originali (non li sovrascrive mai).

- **Motivazione**: la violenza è una proprietà dell'intero frame, non di un singolo oggetto; dipende da cosa la camera inquadra realmente, non dall'intenzione della randomizzazione. Usare le bounding box finali garantisce coerenza con il ground truth su cui si addestrerebbe un modello.
- **Configurazione**: `VIOLENCE_LABELS = ["aggressor", "weapon"]`, `REQUIRE_BOTH = True` (logica AND, configurabile a OR), `METRIC_NAME = "scene_violence"`, `WRITE_SUFFIX = "_violence"`.
- **Funzioni principali**:
  - `build_violence_map(base_path)`: scansiona i `captures_*.json`, estrae le label delle bounding box per frame, applica `all()`/`any()` su `VIOLENCE_LABELS`, restituisce `{sequence_id: 0|1}`.
  - `update_metrics(base_path, violence_map)`: scansiona i `metrics_*.json` (saltando quelli già col suffisso), inserisce la voce `scene_violence` subito dopo l'ultima metrica dello stesso `sequence_id`, scrive un nuovo file con suffisso.
  - `main()`: legge il path da CLI o da `DEFAULT_BASE_PATH`, coordina le funzioni e stampa il riepilogo (conteggio violenti/non violenti, percentuale).
- **Esecuzione**: `python classify_violence.py "<percorso_dataset>"`. Idempotente: rieseguendolo, i file già processati vengono saltati.

### visualize_annotations.py

Script di **ispezione qualitativa** (non fa parte del training set) che genera immagini di verifica a partire da RGB + annotazioni.

- **Dipendenze**: Pillow, numpy (`pip install Pillow numpy`).
- **Configurazione**: `BASE_PATH`, `RGB_FOLDER`, `SEMANTIC_FOLDER`, `INSTANCE_FOLDER` (da aggiornare ad ogni nuovo run, identificato da UUID generato da Unity), `OUTPUT_FOLDER` (creata automaticamente), `LABEL_COLORS` (es. aggressor=rosso, victim=verde, bystander=blu, weapon=arancione), `MAX_FRAMES` (default 50).
- **Funzioni principali**:
  - `load_all_captures(base_path)`: carica e concatena tutti i `captures_*.json`.
  - `draw_bounding_boxes(...)`: disegna rettangoli colorati per label con etichetta testuale sovrapposta.
  - `overlay_semantic(...)` / `overlay_instance(...)`: blend 50/50 (`Image.blend`) tra RGB e maschera, ridimensionata con interpolazione `NEAREST` (per non corrompere i colori discreti delle maschere).
  - `main()`: per ogni frame (fino a `MAX_FRAMES`) genera `<frame_id>_bbox.png`, `<frame_id>_semantic.png`, `<frame_id>_instance.png` nella cartella `visualizations`.

---
