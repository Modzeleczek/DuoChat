CREATE TABLE "Account" ( -- account on a server
  "id" INTEGER,
  "login" TEXT,
  "password" TEXT,
  "public_key" BLOB,
  "private_key" BLOB,
  "nickname" TEXT,
  "image" BLOB,
  PRIMARY KEY("id")
) WITHOUT ROWID;

CREATE TABLE "User" (
  "id" INTEGER,
  "public_key" BLOB, -- user's public key
  "nickname" TEXT,
  "image" BLOB,
  PRIMARY KEY("id")
) WITHOUT ROWID;

CREATE TABLE "Friendship" (
  "account_id" INTEGER,
  "friend_id" INTEGER,
  "alias" TEXT,
  PRIMARY KEY("account_id","friend_id"),
  FOREIGN KEY("account_id") REFERENCES "Account"("id") ON DELETE CASCADE,
  FOREIGN KEY("friend_id") REFERENCES "User"("id") ON DELETE CASCADE
) WITHOUT ROWID;

CREATE TABLE "Conversation" (
  "id" INTEGER,
  "owner_id" INTEGER,
  "name" TEXT,
  PRIMARY KEY("id"),
  FOREIGN KEY("owner_id") REFERENCES "User"("id") ON DELETE SET NULL
) WITHOUT ROWID;

CREATE TABLE "ConversationParticipation" (
  "conversation_id" INTEGER,
  "participant_id" INTEGER,
  "join_time" INTEGER,
  "is_administrator" INTEGER,
  PRIMARY KEY("conversation_id","participant_id"),
  FOREIGN KEY("conversation_id") REFERENCES "Conversation"("id") ON DELETE CASCADE,
  FOREIGN KEY("participant_id") REFERENCES "User"("id") ON DELETE CASCADE
) WITHOUT ROWID;

CREATE TABLE "Message" (
  "id" INTEGER,
  "conversation_id" INTEGER,
  "sender_id" INTEGER,
  "send_time" INTEGER,
  "receive_time" INTEGER,
  "display_time" INTEGER,
  "deleted" INTEGER,
  "plain_content" TEXT, -- decrypted content
  PRIMARY KEY("id"),
  FOREIGN KEY("conversation_id") REFERENCES "Conversation"("id") ON DELETE CASCADE,
  FOREIGN KEY("sender_id") REFERENCES "User"("id") ON DELETE SET NULL
) WITHOUT ROWID;

CREATE TABLE "Attachment" (
  "id" INTEGER,
  "message_id" INTEGER,
  "name" TEXT,
  "type" TEXT,
  "plain_content" BLOB, -- decrypted content
  PRIMARY KEY("id"),
  FOREIGN KEY("message_id") REFERENCES "Message"("id") ON DELETE CASCADE
) WITHOUT ROWID;
