# Recognition prototype: scripts and answer keys

The Python prototype that trained the page analyser's networks (`src/models/`), kept so the
networks can be retrained when more cards are read. What was learned is in
`src/docs/RECOGNITION-LEARNINGS.md`; how read cards feed back is in
`src/docs/IMPORT-WORKFLOW.md`. The app itself uses the C# port in `RetroSchematic.Recognition`,
not these scripts.

## What is here

| Folder | What |
|---|---|
| `scripts/` | page rendering and turning (`pageio.py`, `rotate*.py`), generated training pages (`gen*.py`), the detector (`detnet.py`, `detio.py`), the letter network (`train*.py`), the whole-page draft (`draft.py`, fixes switched on by flags), scoring (`score.py`, `connscore.py`, `stageloss.py`), answer-key rule checks (`keyrules.py`) |
| `loop/` | the shell scripts that run one round (`draftall.sh`, then `scoreall.sh` / `conn.sh`), the work-page set (`set.json`) and the record of every round with its scores and run command (`rounds.json`) |
| `data/keys/` | answer keys for the 10 work pages (NORD-10 book pages 61-63, 65, 66; ND-100 Binder 8 and 96, Input Output System 228 and 237, ND-120 page 41). 9 of 10 pass the rule checks; n10-65 cannot be checked (no 74118 pin table) |
| `data/testkeys/` | answer keys for the SCSI test pages 6, 13, 21 (chips, gates, codes; no nets). Never train on these, or on 2A50 page 64 |
| `data/labels-nearby/`, `data/labels/` | checked letter and box labels, the names given to the letter piles, and the page-64 letter test set |

## Running

- Python 3.11 with torch, opencv-python-headless, onnx, onnxruntime, PyMuPDF; run with `-I`.
- The scripts read the repository's own files relative to their own place. The work folder
  (rendered pages, generated pages, runs, models in training) is NOT in the repository: set
  `RECOG_WORK` to a folder of your own (a temporary folder, never a drive root) for the shell
  scripts.
- The ND-100 books are outside the repository: set `ND100_BINDER`, `ND120_CPU`, `ND_SCSI` and
  `ND100_IOSYS` to the PDFs (see `scripts/books.py`).
- These scripts were written to explore, not as a finished tool: some take their settings as
  constants at the top of the file. `rounds.json` gives the exact command of each round.
