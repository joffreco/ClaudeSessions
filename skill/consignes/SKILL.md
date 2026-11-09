---
name: consignes
description: >-
  Deposit in the Consignes Windows app every procedure the user has to run by hand, so that it can be
  found again without scrolling the conversation. Use this ALWAYS, without being asked, whenever an
  answer hands the user steps to execute themselves: commands to type or paste (a shell, psql, docker
  exec, az, a migration), a test or verification procedure, a recipe to walk through, a deployment or
  rollback runbook; on "donne-moi les commandes", "étape suivante", "guide-moi pas à pas", "qu'est-ce
  que je tape ?", "comment je teste ?". Not for commands Claude runs itself, nor for a single trivial
  command the user already asked for verbatim.
---

# Consignes

The user loses the steps a session gave them once the conversation moves on. Every procedure they must
run themselves is also deposited with the `consigne` command, which files it under the session's
ClaudeSessions tab; the Consignes app shows it with a copy button per code block and tickable steps.

## Rules

- **The answer still shows everything.** The deposit is a copy for later, never a replacement: the
  procedure appears in full in the answer, as it would without this skill.
- **One consigne per procedure**, written to stand alone: one sentence of context (what it tests or
  fixes, which repo, which host), numbered steps, each command in a fenced block with its language,
  exactly as the user must type it, and what a good result looks like. Steps the user ticks off as
  they go are written as tasks (`- [ ] …`).
- **Same procedure, same id.** When a step changes, deposit again with the same `--id`; the app shows
  the new version as unread. Pick a short stable id (`test-widgets`, `deploy-282`).
- **Under review.** A procedure that `answer-review` checks is deposited at once with
  `--status en-revision`, then deposited again with the same id and `--status prete` when the review
  clears it, amended if the review changed it.
- **Done.** When the user says the procedure is done or abandoned, archive it with `consigne done <id>`.

## Callback on a ticked step or a pasted result

- **Mark a step** with `<!-- callback -->` at the end of its task line (`- [ ] Lancer la migration <!-- callback -->`)
  only when you must act the moment the user ticks it: check its result, or give the next step depending on
  what happened. Leave the other steps unmarked: each marked tick costs a session turn. The mark is invisible
  in the answer; the app shows a 🔔 after its box.
- **Wait in the background.** When `put` prints `callbacks:` above 0, run
  `consigne wait <fichier>` (the `fichier:` path `put` printed) with Bash `run_in_background: true`. You are
  woken when it ends:
  - code 0: it printed the step(s) ticked (`Étape 3 cochée : …`). Act on them, then run `wait` again in the
    background.
  - code 4: no tick within the timeout (3600 s by default); run it again if the procedure is still under way.
  - code 3: the consigne was archived; stop.
- **Ask for a result.** When the user must hand back output, such as the rows of a SQL query, put
  `<!-- result: <name> -->` on its own line right after that step (indented under it when the step is a
  list item; `<name>` is short, lowercase, `a-z0-9-`). The app shows a box to paste in and a button
  « Envoyer à la session ». Each field counts in `callbacks:`, so the `wait` above is already running; it
  ends with `Résultat « <name> » collé :` followed by the pasted text. Read it, act on it, and run `wait`
  again. Never ask the user to paste a result in the conversation when a field can take it.
- Keep one `wait` per consigne. A new version under the same id keeps the running `wait`; start one only
  if none is running. Ticks made while no `wait` runs are not lost: the next `wait` gives them at once.

## Command

```bash
consigne put --title "<titre court>" --id <id> [--status en-revision|prete] <<'CONSIGNE'
<Markdown>
CONSIGNE
consigne list          # this session's consignes: id, status, title
consigne done <id>     # archive one
consigne wait <fichier> [--timeout <s>]   # in the background: ends on the next marked tick or sent result (0), archive (3), timeout (4)
```

Use a quoted heredoc delimiter (`<<'CONSIGNE'`) so that nothing in the Markdown is expanded by the shell.
`put` prints the id, the file and the number of marked steps and result fields (`callbacks:`); it fails with code 2 on a bad id, an empty body or a missing title.
