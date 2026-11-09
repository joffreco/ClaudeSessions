#!/bin/bash
# Tests of wsl/consigne. Each case runs the script in a scratch folder with its own CONSIGNES_ROOT,
# CLAUDE_SESSION_TABS and CONSIGNES_WORKSPACE; a copy of bash named claude stands for the tab's claude.
# Usage: wsl/tests/consigne-test.sh — prints one line per case and exits 1 when one fails.

set -u

script="$(cd "$(dirname "$0")/.." && pwd)/consigne"
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT
failures=0
tab_a="11111111-2222-3333-4444-555555555555"
tab_b="66666666-7777-8888-9999-000000000000"
tab_c="aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
tab_d="ffffffff-0000-1111-2222-333333333333"

cp "$(command -v bash)" "$work/claude"
mkdir -p "$work/tabs" "$work/projet"
printf '{"tab_id":"%s","session_id":"sess-1234","cwd":"/x","transcript_path":"/y"}' "$tab_a" > "$work/tabs/$tab_a.json"
cat > "$work/workspace.json" <<EOF
{"version":1,
 "windows":[{"tabs":[{"id":"$tab_a","title":"682 runner1","cwd":"/x"},{"id":"$tab_c","title":"Claude Code"},{"id":"$tab_d","title":null}]}],
 "closedTabs":[{"tab":{"id":"$tab_b","title":"onglet fermé"},"windowId":"w","closedAt":"2026-11-04T00:00:00Z"}]}
EOF

export CONSIGNES_ROOT="$work/root" CLAUDE_SESSION_TABS="$work/tabs" CONSIGNES_WORKSPACE="$work/workspace.json"
unset CLAUDE_SESSIONS_TAB CLAUDE_SESSIONS_TAB_PID

check() {
  if [ "$2" = "$3" ]; then
    printf 'ok   %s\n' "$1"
  else
    printf 'FAIL %s\n     attendu: %s\n     obtenu:  %s\n' "$1" "$3" "$2"
    failures=$((failures + 1))
  fi
}

field() {
  sed -n "2,/^---\$/ { s/^$2: //p }" "$1" | head -n 1 | jq -r .
}

in_tab() {
  local tab="$1"
  shift
  (cd "$work/projet" && CLAUDE_SESSIONS_TAB="$tab" "$work/claude" -c 'CLAUDE_SESSIONS_TAB_PID=$$ "$0" "$@"; exit $?' "$script" "$@")
}

out=$(cd "$work/projet" && printf 'Étape 1\n' | "$script" put --title "Tester l'export" --id export)
file="$CONSIGNES_ROOT/_sans-onglet/projet/export.md"
check "hors onglet : rangée sous _sans-onglet/<dossier du cwd>" "$([ -f "$file" ] && echo oui)" "oui"
check "hors onglet : tab_id vide" "$(field "$file" tab_id)" ""
check "la sortie donne l'id" "$(printf '%s' "$out" | head -n 1)" "id: export"
check "statut par défaut prete" "$(field "$file" status)" "prete"
check "le corps suit l'en-tête" "$(tail -n 1 "$file")" "Étape 1"

title=$'Titre "piégé": avec\nretour et \\ barre'
(cd "$work/projet" && printf 'x\n' | "$script" put --title "$title" --id piege) > /dev/null
check "un titre piégé ne casse pas l'en-tête" "$(field "$CONSIGNES_ROOT/_sans-onglet/projet/piege.md" title)" "$title"

created=$(field "$file" created)
sleep 1
(cd "$work/projet" && printf 'Étape 2\n' | "$script" put --title "Tester l'export" --id export --status en-revision) > /dev/null
check "même id : created gardé" "$(field "$file" created)" "$created"
check "même id : corps remplacé" "$(tail -n 1 "$file")" "Étape 2"
check "même id : statut mis à jour" "$(field "$file" status)" "en-revision"
check "même id : updated changé" "$([ "$(field "$file" updated)" != "$created" ] && echo oui)" "oui"

(cd "$work/projet" && printf 'x\n' | "$script" put --title t --id ../evil 2> /dev/null)
check "un id avec un chemin est refusé (code 2)" "$?" "2"
check "rien n'est écrit hors de la racine" "$(find "$work" -name 'evil*' | wc -l)" "0"

(cd "$work/projet" && printf '  \n' | "$script" put --title t --id vide 2> /dev/null)
check "un corps vide est refusé (code 2)" "$?" "2"

(cd "$work/projet" && printf 'x\n' | "$script" put --title t --status autre 2> /dev/null)
check "un statut inconnu est refusé (code 2)" "$?" "2"

out=$(cd "$work/projet" && printf 'x\n' | "$script" put --title "Procédure : déploiement")
check "sans --id : id tiré du titre" "$(printf '%s' "$out" | head -n 1 | sed 's/-[0-9]*$//')" "id: procedure-deploiement"

printf 'x\n' | in_tab "$tab_a" put --title t --id dans-onglet > /dev/null
file="$CONSIGNES_ROOT/$tab_a/dans-onglet.md"
check "dans un onglet : rangée sous le tab_id" "$([ -f "$file" ] && echo oui)" "oui"
check "dans un onglet : tab_id" "$(field "$file" tab_id)" "$tab_a"
check "dans un onglet : titre de l'onglet ouvert" "$(field "$file" tab_title)" "682 runner1"
check "dans un onglet : session_id du record" "$(field "$file" session_id)" "sess-1234"
check "dans un onglet : cwd" "$(field "$file" cwd)" "$work/projet"

printf 'x\n' | in_tab "$tab_b" put --title t --id ferme > /dev/null
check "onglet seulement dans closedTabs : son titre" "$(field "$CONSIGNES_ROOT/$tab_b/ferme.md" tab_title)" "onglet fermé"
check "sans record de session : session_id vide" "$(field "$CONSIGNES_ROOT/$tab_b/ferme.md" session_id)" ""

printf 'x\n' | in_tab "$tab_c" put --title t --id generique > /dev/null
check "titre « Claude Code » ignoré" "$(field "$CONSIGNES_ROOT/$tab_c/generique.md" tab_title)" ""

printf 'x\n' | in_tab "$tab_d" put --title t --id nul > /dev/null
check "titre null ignoré" "$(field "$CONSIGNES_ROOT/$tab_d/nul.md" tab_title)" ""

(cd "$work/projet" && printf 'x\n' | CLAUDE_SESSIONS_TAB="$tab_a" CLAUDE_SESSIONS_TAB_PID=1 "$script" put --title t --id imbrique) > /dev/null
check "claude imbriqué (PID différent) : _sans-onglet" "$([ -f "$CONSIGNES_ROOT/_sans-onglet/projet/imbrique.md" ] && echo oui)" "oui"

printf 'x\n' | CONSIGNES_WORKSPACE="$work/absent.json" in_tab "$tab_a" put --title t --id sans-workspace > /dev/null
check "workspace.json absent : dépôt fait, titre vide" "$(field "$CONSIGNES_ROOT/$tab_a/sans-workspace.md" tab_title)" ""

check "list : id, statut et titre" "$(in_tab "$tab_a" list | grep '^dans-onglet')" "$(printf 'dans-onglet\tprete\tt')"
in_tab "$tab_a" done dans-onglet > /dev/null
check "done : archivée" "$([ -f "$CONSIGNES_ROOT/$tab_a/archive/dans-onglet.md" ] && [ ! -f "$CONSIGNES_ROOT/$tab_a/dans-onglet.md" ] && echo oui)" "oui"
in_tab "$tab_a" done inconnue 2> /dev/null
check "done d'un id inconnu : erreur" "$?" "1"

dir="$CONSIGNES_ROOT/_sans-onglet/projet"
out=$(cd "$work/projet" && printf '%s\n' '- [ ] un <!-- callback -->' '- [x] deux <!--callback-->' '1. [ ] trois' '- [ ] quatre <!-- note -->' '```' '- [ ] code <!-- callback -->' '```' | "$script" put --title t --id rappel)
check "put : callbacks compte les tâches marquées, hors code" "$(printf '%s' "$out" | sed -n 's/^callbacks: //p')" "2"
check "put : callbacks à 0 sans marque" "$(cd "$work/projet" && printf 'x\n' | "$script" put --title t --id sans-rappel | sed -n 's/^callbacks: //p')" "0"

(cd "$work/projet" && "$script" wait rappel --timeout 1) > /dev/null
check "wait : timeout sans coche (code 4)" "$?" "4"
printf '%s\n' '{"index":0,"text":"un","at":"2026-11-04T11:00:00-05:00"}' >> "$dir/rappel.events.jsonl"
printf '%s' '{"index":2,"text":"incomplet"' >> "$dir/rappel.events.jsonl"
out=$("$script" wait "$dir/rappel.md" --timeout 5)
check "wait : rend la coche par le chemin du fichier (code 0)" "$?" "0"
check "wait : texte de la coche" "$out" "Étape 1 cochée : un"
check "wait : une ligne incomplète n'est pas rendue" "$(cat "$dir/rappel.events.seen")" "1"
printf '%s\n' ',"at":"x"}' >> "$dir/rappel.events.jsonl"
check "wait : la ligne complétée est rendue ensuite" "$("$script" wait "$dir/rappel.md" --timeout 5)" "Étape 3 cochée : incomplet"
(sleep 1; printf '%s\n' '{"index":1,"text":"deux","at":"x"}' >> "$dir/rappel.events.jsonl") &
check "wait : attend la coche suivante" "$("$script" wait "$dir/rappel.md" --timeout 10)" "Étape 2 cochée : deux"
(cd "$work/projet" && "$script" done rappel > /dev/null)
check "done : fichiers d'événements archivés" "$(ls "$dir/archive" | grep '^rappel' | tr '\n' ' ')" "rappel.events.jsonl rappel.events.seen rappel.md "
"$script" wait "$dir/rappel.md" --timeout 1 > /dev/null 2>&1
check "wait d'une consigne absente : erreur (code 1)" "$?" "1"
(cd "$work/projet" && printf 'x\n' | "$script" put --title t --id archivee > /dev/null)
(sleep 1; mv "$dir/archivee.md" "$dir/archive/") &
check "wait : consigne archivée pendant l'attente (code 3)" "$("$script" wait "$dir/archivee.md" --timeout 10; echo "code $?")" "$(printf 'consigne archivee archivée\ncode 3')"
"$script" wait 'x/../evil' > /dev/null 2>&1
check "wait : chemin sans .md refusé (code 2)" "$?" "2"

(cd "$work/projet" && printf 'x\n' | "$script" put --id sans-titre 2> /dev/null)
check "put sans --title refusé (code 2)" "$?" "2"
(cd "$work/projet" && printf 'x\n' | "$script" put --title t --id Majuscule 2> /dev/null)
check "un id en majuscules est refusé (code 2)" "$?" "2"
(cd "$work/projet" && "$script" done '../evil' 2> /dev/null)
check "done d'un id avec un chemin est refusé (code 2)" "$?" "2"
(cd "$work/projet" && "$script" wait rappel --timeout abc 2> /dev/null)
check "wait : un timeout non numérique est refusé (code 2)" "$?" "2"
(cd "$work/projet" && "$script" frobnicate 2> /dev/null)
check "une commande inconnue est refusée (code 2)" "$?" "2"
check "list d'un onglet sans consigne : sortie vide" "$(in_tab "00000000-0000-0000-0000-000000000001" list | wc -l | tr -d ' ')" "0"

out=$(cd "$work/projet" && printf '%s\n' '- [ ] un <!-- callback -->' '' '<!-- result: comptes -->' '  <!-- result -->' '```' '<!-- result: code -->' '```' | "$script" put --title t --id resultat)
check "put : un champ de résultat compte comme un rappel, hors code" "$(printf '%s' "$out" | sed -n 's/^callbacks: //p')" "3"
printf '%s\n' '{"kind":"result","name":"comptes","text":"id | n\n1 | 42","at":"x"}' >> "$dir/resultat.events.jsonl"
check "wait : rend le résultat collé, sur plusieurs lignes" "$("$script" wait "$dir/resultat.md" --timeout 5)" "$(printf 'Résultat « comptes » collé :\nid | n\n1 | 42')"

check "aucun fichier temporaire laissé" "$(find "$CONSIGNES_ROOT" -name '.tmp.*' | wc -l)" "0"

[ "$failures" -eq 0 ] || { printf '%s échec(s)\n' "$failures"; exit 1; }
printf 'tous les cas passent\n'
