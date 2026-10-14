# Selected Ticket and Starlight real-game checks

Build the dual package first with `tools/build_dual_branch.ps1`. Use Python 3, .NET 9, FFmpeg and the repository's isolated stable/beta development snapshots. The temporary observer and rendering guard are installed only in fresh clones; they must not be shipped with Karen.

```powershell
python tools/vfx/starlight/verify.py stable --output artifacts/starlight-check-stable
python tools/vfx/starlight/verify.py beta --output artifacts/starlight-check-beta
```

Each output must be a new directory. Clones use distinct user data, MCP ports 18947/18948 and a private desktop. Game binaries are read-only hardlinks. The harness never switches the user's desktop or installs into Steam. It continues the saved Karen run, starts a controlled combat, prepares real card/pile fixtures and verifies native gameplay results.

`--phase all` covers Ticket cast/return, 0/2/4 Promise draws, Act 1 upgraded draw, Act 2 3/5 plays, sustained hover/unhover, presentation cancellation, Fast/Instant and cleanup. `--phase remaining` reruns the cancellation/speed tail. `--phase final` focuses on actual drawn-card edge binding and combat-ending hover ownership, with both Starlight powers active.

`stage_probe cancel` cancels visual nodes only; it does not test a real queued card cancellation. Observation records actual cue creation, results, background parent, play indices and ownership/depletion/ending frames. Screenshots before/after movement plus `karen_check_hand` verify model/view agreement. Outputs contain native recordings, JSON actions, probe results and build hashes. A successful script requires `results.json`; inspect `recording.json` independently and audit logs rather than equating a process exit with clean runtime diagnostics.

Latest recorded results and limitations: `docs/design/six-card-vfx-2026-10-13/starlight-runtime-validation.md`.
