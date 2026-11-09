---
name: breakpoint-consignes
description: >-
  Prepare and drive a hand-test suite at the breakpoint for a change-set before it is staged: inventory
  every changed hunk, prove each UI gesture reaches the code, pick concrete test data, deposit one
  consigne per gesture in the Consignes app (breakpoints, gesture, expected values, forced refusal,
  callback, positive staging), watch the real SQL through Extended Events and give a green light per
  part, then close with a coverage audit and cleanup. Use this on `/breakpoint-consignes`, or when the
  user asks to "walk the branch at the breakpoint", "prépare les consignes de test au breakpoint",
  "suite de tests au breakpoint", "je veux voir chaque bloc avant de stager", or unstages a branch's
  production files to re-stage them block by block. Unit tests are out of the walk (they belong to
  `precommit`). Builds on the `consignes` skill.
---

# Breakpoint consignes

The user unstages a branch's production files and re-stages them block by block, once each block is
seen working at the breakpoint. This skill builds the suite of consignes that drives the walk, checks
every gesture against the SQL the server really sent, and tells the user what to stage after each part.
Every claim in a consigne must be true on the first try: a wrong line, a wrong variable name or an
unreachable gesture costs a round trip.

Read the `consignes` skill first; its rules (`put`, ids, statuses, `<!-- callback -->`, `wait`) apply.
Consignes are written in French, with English programming terms.

## 1. Prepare the suite

1. **Inventory.** List every changed hunk outside tests, with its function context:
   `git diff -U0 --function-context -- . ':!**/*.test.*' ':!**/*.spec.*' ':!**/__tests__/**'` (plus
   staged hunks if any: `git diff --cached -U0`). Keep a table hunk → file:lines → function → gesture.
2. **Map hunks to gestures.** One consigne per UI gesture, not per method: a click crosses gates,
   scopes and helpers. Number `00`…`NN` in the title and the id (`bp-01-ouvrir-conversation`); a
   consigne inserted later takes a letter (`bp-03b-…`).
3. **`00` = blocks already covered earlier in the session.** They still get a walk (the user wants to
   see them at work), split into parts A, B, C.
4. **Prove each gesture reaches the code**, from the front-end source, never from assumptions. Traps
   met before:
   - a menu that never offers the action (the action absent from the menu arrays);
   - a button gated by a PBX `ckrest.ini` flag (`general.sharedlink`);
   - a branch that only runs for a status the lookup never returns;
   - a value that only exists in an SMS payload;
   - an input mask that refuses some values (phone mask: no 0 in the first digit of area code and
     exchange, e.g. `1 (899) 799-9999` is refused).
5. **Unreachable code** (no UI trigger, dead function, impossible branch, support-only / god-mode path)
   is listed apart, with its unit-test coverage. The user decides whether it is committed; never force
   it into a consigne. A console `fetch` or socket emit can stand in for a missing UI trigger when the
   user wants to see it.
6. **Pick concrete test data in the local DB** and write it in the consigne: ids by status, phone
   numbers that pass the UI mask, the company, the users. Re-check the status just before the user acts
   on a consigne: earlier ones change it.
   **Name every record the way the UI shows it.** A consigne that asks the user to open, find or click
   a record names it by what the screen displays (for a chat conversation: the customer's name and
   phone number as the list shows them, the company, the status, the assignee), with a copy block of
   the value to type in the screen's search. The database id (`cc_id`, `cm_id`, `cs_id`) follows in
   parentheses, for the capture only: the UI never shows it, and a bare id sends the user back to ask
   (COM-682 point 9: « ouvrir la conversation 130145 »).
7. **Verify before depositing**, in the code: every breakpoint line, every variable a refusal step
   edits, every expected value. Under ts-node + SWC a breakpoint on a lone `await` line may not bind;
   prefer the line before.
   **A thread through every hunk; a breakpoint only to intervene.** Each hunk of the inventory is
   crossed by the execution thread of a real gesture (or a chain of gestures) in the consigne that stages
   it, including hunks that only pass a parameter along and every private helper of a new file. The
   consigne lists, per gesture, the hunks the thread crosses in order and the observed value that proves
   the passage: a statement in the SQL capture, a log line, an HTTP status or body, what the UI shows. A
   hunk reached by a normal gesture gets no breakpoint. A breakpoint exists only to intervene when a
   block is out of the normal path: force a refusal branch, a `catch`, a parameter, an empty list, by
   changing a value at the stop; the consigne gives the line, the exact Debug Console expression, and
   the proof that the thread entered the block. The only hunks no thread crosses are written in the
   consigne with their reason: an import, a docstring, a type or DTO with no runtime code, an
   unreachable branch. A file is staged whole only when every one of its hunks is crossed or excused;
   walk the inventory hunk by hunk against the threads before depositing (COM-682: `MessagesPdf.collect`
   and six private methods of `DocumentScope` were staged whole without being shown).
8. **Present the plan** (inventory table, gestures, unreachable list, data) to the user before
   depositing.

## 2. Write a consigne

Structure, in this order:

1. One context sentence: the gesture, the route or socket event it triggers, the repo, the ticket.
2. A task checking that the debug process started after the last code change. Claude checks it first with
   `scripts/debug-start.sh <container> <repo_dir>` (prints `OK` or `RESTART`); the task tells the user to
   restart otherwise.
3. The threads, one per gesture: the gesture, the hunks it crosses in order, and the proof observed for
   each (SQL, log, HTTP, UI).
4. The interventions, one per block out of the normal path: the breakpoint, the expression to type at the
   stop, the block it makes the thread enter, and the proof. The user removes all breakpoints at the end
   of each consigne, so a consigne never relies on an earlier one's breakpoints.
5. Forced refusals and errors are interventions of point 4.
6. A callback task: `- [ ] Cocher cette étape : Claude vérifie dans la capture SQL … <!-- callback -->`.
7. A staging task.

With several gestures, split into bold parts **A.**, **B.**…, each with its own refusal, callback and
staging. Model: `references/example-consigne.md`.

Rules:

- **Every list is a bullet list**, never a comma-separated line, and **every bullet carries a
  ```` ```text ```` copy block** with exactly what the user pastes: a breakpoint `file:line`, a Debug
  Console expression, a value to type, a path to stage. Only group headers (« Blocs : »,
  « Fichiers entiers : ») have no block. The app shows a « Copier » button on each nested block.
- **Refusal at the innermost breakpoint**, on the gate's own parameter (`cc_id = 999999999` at the
  gate's line): a caller's local is out of scope unless its frame is selected in Call Stack. Out-of-scope
  values: an id that does not exist (`999999999`), a company `999999`, a user of another domain. State
  what the refusal produces (exception constructor hit, `Logger.warn`, HTTP status, socket
  `failedcallback`) and that no write follows.
- **Staging is positive only.** One bullet per hunk, with its line range and what it is, copy block
  `path:startLine` so the paste jumps there; a fully tested file as its bare path (« + » in the changes
  list). Never « all remaining blocks except X ». A file shared by several gestures is staged whole only
  at the last consigne that touches it. Recall the gesture once per consigne: VS Code diff, select the
  lines, right click, « Stage Selected Ranges ».
- **Warn before any gesture with an outbound side effect** (section 4).

## 3. Deposit, run and check

**Deposit order.** The app sorts by `updated`, newest first, to the second. Deposit from the last
consigne to `00` with `sleep 1.1` between `put`s, so that `00` is on top. Redepositing one consigne moves
it to the top: redeposit every following one too, in the same reverse order, when the order matters.

**Amending a consigne under way.** Ticks are stored by task index in `<id>.events.jsonl`: insert new
steps only after the ones already ticked, never before. A new check gets its own callback. **Never `put`
an archived consigne**: `put` recreates it active. Read archives, never redeposit them.

**SQL capture (SQL Server).** Claude watches the SQL, not the user (`DB_LOGGING` is too noisy for him).
Before the first consigne:

```bash
scripts/xe-start.sh <session_name> <database> <repo_dir>   # e.g. com682_sql userportal ~/Sources/comunikator
```

At each callback:

```bash
scripts/xe-read.sh <session_name> <repo_dir> [minutes] [pattern]
scripts/xe-read.sh <session_name> <repo_dir> 10 'chat_conversation|users'
scripts/xe-read.sh <session_name> <repo_dir> 10 '' fail   # only statements whose result is not OK
```

Scripts live in this skill's `scripts/` folder; `sqlcmd.sh` takes the `sa` password from the repo's
`.env` (`DB_PASS*`) and the container from `SQL_CONTAINER` (default `comunik-sql-db`). The pattern is a
`grep -E` alternation (`a|b`, never `a\|b`, which silently matches nothing). For a
PostgreSQL repo, use the engine's statement logging instead (see `sql-capture`) and say so.

**Waiting.** One background script waits on every open consigne:

```bash
scripts/wait-all.sh <consignes_tab_dir> 'bp-*'
```

Launch it with Bash `run_in_background: true`, and relaunch it after each wake. Never `& disown` it from
a foreground shell: no wake comes back. Check its glob matches every id (`bp-03b-…` was once missed by
`bp-[0-9][0-9]-*`). The tab folder is the directory of the `fichier:` path `put` printed.

**Green light**, at each callback, per gesture:

- the gate read, with its `domain_id` bound (quote the `WHERE`);
- each write, with its `WHERE`;
- the absence of any write after the refusal;
- what the capture cannot show (a `Logger.warn` seen only in the Debug Console, a refusal decided before
  any query);
- what to stage now, repeating the consigne's staging list.

If the capture contradicts the consigne, say so and amend before the user stages.

**Pre-existing bug blocking the UI** in another repo: say it is pre-existing (cite the commit, and that
it is also on `staging`), open a Jira ticket on the user's word, and only on the user's explicit OK apply
a minimal local patch that is never staged or committed; give the `git checkout -- <file>` line to
revert it at the end.

**Support-only / god-mode paths** the user cannot exercise are judged by reading the code and its unit
tests, said so plainly.

## 4. Outbound side effects (comunikator)

- `.env` points at a real SMS gateway. Only the exact string `SIMULATE_NOTIFICATIONS="true"` simulates.
  The flag is read at import: a change needs a debug restart. With `"false"`, mail goes to the local
  Mailpit (`http://localhost:8025`) and **SMS go out for real**: warn in the consigne before any gesture
  that sends one, and check the value before the walk.
- `jest.setup.ts` hard-sets `SIMULATE_NOTIFICATIONS='false'`, `SMS_API=''`, `SMS_AUTH_KEY=''` so a
  developer's `.env` never changes test outcomes nor reaches the gateway.
- Incoming SMS webhook (`POST /webhooks/incomingsms`): `msgid` numeric, `from` a new number each time,
  or no new conversation is created. Generate both from `date +%s` in the curl.

## 5. Close

1. **Coverage audit**: every staged non-test hunk mapped to the consigne that walked it; the rest listed
   by reason (no UI trigger, impossible branch, dead code, support-only, migration, docstring only).
   Check `git diff` (unstaged non-test hunks left) and `git diff --cached` against the table.
2. **Cleanup**: `scripts/xe-stop.sh <session_name> <repo_dir>` (drops the XE session and its `.xel` files), stop
   the background waits (`TaskStop`), archive what remains with `consigne done`.
3. **Remind the user** of: local patches to revert (with the `git checkout --` line), the current
   `SIMULATE_NOTIFICATIONS` value, files left unstaged on purpose (`jest.setup.ts`, unit tests), and
   offer to clean the local test data created by the walk.
