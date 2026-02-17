Puzzle made so player has to fill as much area as he can using available puzzles
use middle mouse to move perspective, left button to drag puzzles, scroll to zoom

## New V2 features
- Snap can be toggled live in-game (`Snap` switch).
- Best local result is persisted per puzzle key (`level + seed + rulesVersion`).
- `Restore Best` restores your best piece placement snapshot.
- Coverage, local best, world record, and stars (`90/95/98% of reference record`) are shown in HUD.
- Offline-first record sync queue: scores are queued locally and retried with backoff.

## Record server
- Minimal API project: `FSquir.Api`.
- PostgreSQL schema with EF Core migrations (`PlayerBestScore`, `WorldRecord`, `ScoreSubmissionLog`).
- Endpoints:
  - `GET /api/v1/records/{level}/{seed}/{rulesVersion}`
  - `POST /api/v1/scores`

### Run locally with Docker
```bash
docker compose up --build
```

API default URL for app sync: `http://localhost:5180`.
