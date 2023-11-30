DROP TABLE IF EXISTS "Account";
CREATE TABLE "Account" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "login" TEXT NOT NULL,

  /* Jeżeli będzie logowanie lub resetowanie klucza publicznego hasłem.
  "password_digest" BLOB NOT NULL, */

  "public_key" BLOB NOT NULL,

  /* Lub domyślnie dopuszczamy NULL i wtedy traktujemy login jako pseudonim,
  a jeżeli nickname nie jest nullem, to używamy go jako alias loginu.
  "nickname" TEXT NOT NULL, */

  /* Lub przechowujemy obrazy w katalogu w systemie plików na serwerze.
  "image" BLOB, */

  "is_blocked" INTEGER NOT NULL,

  UNIQUE("login"),
  UNIQUE("public_key")
);

DROP TABLE IF EXISTS "Conversation";
CREATE TABLE "Conversation" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,

  /* Po usunięciu rekordu właściciela z tabeli Account (różne możliwości):
  - konwersacja pozostaje bez właściciela
  - przypisujemy jako właściciela najstarszego członka (lub administratora)
    wciąż obecnego w konwersacji.
  - nie usuwamy rekordów z tabeli Account, ale oznaczamy je jako usunięte i
    pozostawiamy "usuniętego" właściciela konwersacji; dzięki temu można
    "odusunąć" rekord właściciela w tabeli Account i automatycznie dalej będzie on
    właścicielem konwersacji
  */
  "owner_id" INTEGER,
  "name" TEXT NOT NULL,
  FOREIGN KEY("owner_id") REFERENCES "Account"("id") ON DELETE SET NULL
);

DROP TABLE IF EXISTS "ConversationParticipation";
CREATE TABLE "ConversationParticipation" (
  "conversation_id" INTEGER NOT NULL,
  "participant_id" INTEGER NOT NULL,
  "join_time" INTEGER NOT NULL,
  "is_administrator" INTEGER NOT NULL,
  PRIMARY KEY("conversation_id","participant_id"),
  FOREIGN KEY("conversation_id") REFERENCES "Conversation"("id") ON DELETE CASCADE,
  FOREIGN KEY("participant_id") REFERENCES "Account"("id") ON DELETE CASCADE
) WITHOUT ROWID;

DROP TABLE IF EXISTS "Message";
CREATE TABLE "Message" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "conversation_id" INTEGER NOT NULL,

  -- Jeżeli usuniemy nadawcę z tabeli Account, to wiadomość ustawiamy na NULL.
  "sender_id" INTEGER,

  -- Czas unixowy, czyli liczba milisekund od 1 stycznia 1970.
  "send_time" INTEGER NOT NULL,

  /* Jeżeli będzie oznaczanie jako usuniętych ("usuwanie") wiadomości.
  "is_deleted" INTEGER NOT NULL, */

  FOREIGN KEY("conversation_id") REFERENCES "Conversation"("id") ON DELETE CASCADE,
  
  /* Po usunięciu rekordu nadawcy z tabeli Account, wiadomość
  pozostaje bez nadawcy, ale uczestnicy konwersacji mogą ją odczytać. */
  FOREIGN KEY("sender_id") REFERENCES "Account"("id") ON DELETE SET NULL
);

DROP TABLE IF EXISTS "EncryptedMessageContent";
CREATE TABLE "EncryptedMessageContent" (
  "message_id" INTEGER NOT NULL,
  "receiver_id" INTEGER NOT NULL,

  -- NULL, gdy odbiorca jest offline i jeszcze nie odebrał wiadomości.
  "receive_time" INTEGER,
  "content" BLOB,
  
  /* Jeżeli będzie wyświetlanie czasu wyświetlenia wiadomości.
  "display_time" INTEGER NOT NULL, */
  PRIMARY KEY("message_id","receiver_id"),
  FOREIGN KEY("message_id") REFERENCES "Message"("id") ON DELETE CASCADE,
  FOREIGN KEY("receiver_id") REFERENCES "Account"("id") ON DELETE CASCADE
) WITHOUT ROWID;

DROP TABLE IF EXISTS "Attachment";
CREATE TABLE "Attachment" (
  "id" INTEGER PRIMARY KEY AUTOINCREMENT,
  "message_id" INTEGER NOT NULL,
  "name" TEXT NOT NULL,
  "type" TEXT NOT NULL,
  FOREIGN KEY("message_id") REFERENCES "Message"("id") ON DELETE CASCADE
);

DROP TABLE IF EXISTS "EcryptedAttachmentContent";
CREATE TABLE "EcryptedAttachmentContent" (
  "attachment_id" INTEGER NOT NULL,
  "receiver_id" INTEGER NOT NULL,

  /* Treść można przechowywać w poniższym blobie lub w systemie plików serwera
  w katalogu np. database/attachments/<id załącznika>/<id odbiorcy>.
  "content" BLOB, */
  PRIMARY KEY("attachment_id","receiver_id"),
  FOREIGN KEY("attachment_id") REFERENCES "Attachment"("id") ON DELETE CASCADE,
  FOREIGN KEY("receiver_id") REFERENCES "Account"("id") ON DELETE CASCADE
) WITHOUT ROWID;

DROP TABLE IF EXISTS "ClientIPBlock";
CREATE TABLE "ClientIPBlocks" (
  "ip_address" INTEGER PRIMARY KEY
);
