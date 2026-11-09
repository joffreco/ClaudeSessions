# Example consigne (COM-682, comunikator, 2026-11-04)

Body of `bp-01-ouvrir-conversation`, as deposited with `consigne put --title "01 Ouvrir une conversation (COM-682)" --id bp-01-ouvrir-conversation`. The staging step predates the `path:startLine` rule: new consignes give one block per hunk with `path:startLine`.

````markdown
Cliquer sur une conversation de la liste du Chat (comunikator, COM-682). Le clic émet le socket `FETCH_MESSAGE` et appelle `GET /chat/conversation/actions/:cc_id` ; les deux passent par `ConversationGate` et `ConversationScope`.

- [ ] « Debug (ts-node + SWC) » doit avoir été lancé après le dernier changement de code ; sinon, l'arrêter et le relancer.
- [ ] Mettre les breakpoints :
  - `admitForSocket` de `FETCH_MESSAGE`
    ```text
    src/sockets/Messages.ts:207
    ```
  - `admit` de `GET /conversation/actions/:cc_id`
    ```text
    src/routes/ChatApis/Conversation.ts:667
    ```
  - `guard`
    ```text
    src/modules/chat/scope/ConversationGate.ts:65
    ```
  - `resolve`
    ```text
    src/modules/chat/scope/ConversationScope.ts:40
    ```
  - `resolveForActor`
    ```text
    src/modules/chat/scope/ConversationScope.ts:62
    ```
  - `parse`
    ```text
    src/utils/PositiveId.ts:14
    ```
  - `updateReadbyConvid`
    ```text
    src/modules/chat/Messages.ts:113
    ```
  - constructeur de `ConversationOutOfScope` (pour les refus)
    ```text
    src/modules/chat/errors/ConversationOutOfScope.ts:10
    ```
- [ ] Cliquer sur une conversation non lue. Le debugger passe deux fois par `guard` (`FETCH_MESSAGE`, puis la route) ; `resolve` reçoit ton `domain_id` et le `cc_id` ; `resolveForActor` admet la conversation (admin : tout ; autre rôle : assignée, `created` ou `unblocked`). `updateReadbyConvid` marque les messages lus.
- [ ] Refus : au breakpoint de `Messages.ts:207`, taper `obj.cc_id = 999999999` dans la Debug Console, puis continuer. `ConversationOutOfScope` est lancée (`src/modules/chat/errors/ConversationOutOfScope.ts:10`), `guard` écrit un `Logger.warn` et renvoie `null`, et `FETCH_MESSAGE` répond `failedcallback`.
- [ ] Refus HTTP : recliquer, et au breakpoint de `Conversation.ts:667` taper `cc_id = '999999999'`. La route répond 404 `chat:error.conversation.notfound`.
- [ ] Cocher cette étape : Claude vérifie dans la capture SQL la lecture bornée au domaine et l'update des messages lus. <!-- callback -->
- [ ] Après le feu vert de Claude, stager. Pour stager un bloc : ouvrir le diff du fichier dans VS Code, sélectionner les lignes du bloc, clic droit, « Stage Selected Ranges ». Pour un fichier entier : « + » dans la liste des changements.
  - Blocs :
    - `FETCH_MESSAGE` (198-273)
      ```text
      src/sockets/Messages.ts
      ```
    - `GET /conversation/actions/:cc_id` (657-696)
      ```text
      src/routes/ChatApis/Conversation.ts
      ```
  - Fichiers entiers :
    - Messages.ts
      ```text
      src/modules/chat/Messages.ts
      ```
    - ConversationScope.ts
      ```text
      src/modules/chat/scope/ConversationScope.ts
      ```
    - ConversationOutOfScope.ts
      ```text
      src/modules/chat/errors/ConversationOutOfScope.ts
      ```
    - ConversationScopePort.ts
      ```text
      src/modules/chat/interface/ConversationScopePort.ts
      ```
    - ConversationActorDto.ts
      ```text
      src/modules/chat/dto/ConversationActorDto.ts
      ```
    - ScopedConversationDto.ts
      ```text
      src/modules/chat/dto/ScopedConversationDto.ts
      ```
````
