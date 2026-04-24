# Codex hook logger

Local proof of concept for Codex lifecycle hooks.

The hook command reads the JSON payload from stdin and writes local logs to:

```text
hooks/logs/
```

Useful files:

- `YYYY-MM-DD-all.jsonl` - every hook payload as JSON Lines.
- `YYYY-MM-DD-<EventName>.jsonl` - payloads split by hook event.
- `last.json` - last payload in pretty JSON.
- `last-<EventName>.json` - last payload for a specific hook event.

The logger intentionally writes nothing to stdout. Codex interprets stdout from
hooks as control output, so logging should stay file-only.
