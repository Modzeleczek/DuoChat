CREATE TABLE "Server" (
  "public_key" BLOB,
  "ip_address" BLOB,
  "port" INTEGER,
  "name" TEXT,
  PRIMARY KEY("public_key")
) WITHOUT ROWID;

CREATE TABLE "Account" ( -- account on a server
  "server_public_key" BLOB,
  "login" TEXT,
  "password" TEXT,
  "public_key" BLOB,
  "private_key" BLOB,
  "nickname" TEXT,
  "image" BLOB,
  PRIMARY KEY("server_public_key","login"),
  FOREIGN KEY("server_public_key") REFERENCES "Server"("public_key") ON DELETE CASCADE
) WITHOUT ROWID;

CREATE TABLE "User" (
  "server_public_key" BLOB,
  "login" TEXT, -- user's login
  "public_key" BLOB, -- user's public key
  "nickname" TEXT,
  "image" BLOB,
  PRIMARY KEY("server_public_key","login"),
  FOREIGN KEY("server_public_key") REFERENCES "Server"("public_key") ON DELETE CASCADE
) WITHOUT ROWID;

CREATE TABLE "Friendship" (
  "server_public_key" BLOB,
  "account_login" TEXT,
  "friend_login" TEXT,
  "friend_alias" TEXT,
  PRIMARY KEY("server_public_key","account_login","friend_login"),
  FOREIGN KEY("server_public_key","account_login") REFERENCES "Account"("server_public_key","login") ON DELETE CASCADE,
  FOREIGN KEY("friend_login") REFERENCES "User"("login") ON DELETE CASCADE
) WITHOUT ROWID;

CREATE TABLE "Conversation" (
  "server_public_key" BLOB,
  "id" INTEGER,
  "owner_login" TEXT,
  PRIMARY KEY("server_public_key","id"),
  FOREIGN KEY("server_public_key") REFERENCES "Server"("public_key") ON DELETE CASCADE
) WITHOUT ROWID;

CREATE TABLE "ConversationParticipation" (
  "server_public_key" BLOB,
  "conversation_id" INTEGER,
  "participant_login" TEXT,
  PRIMARY KEY("server_public_key","conversation_id","participant_login"),
  FOREIGN KEY("server_public_key","conversation_id") REFERENCES "Conversation"("server_public_key","id") ON DELETE CASCADE,
  FOREIGN KEY("participant_login") REFERENCES "User"("login") ON DELETE CASCADE
) WITHOUT ROWID;

CREATE TABLE "Message" (
  "server_public_key" BLOB,
  "conversation_id" INTEGER,
  "id" INTEGER, -- message's id in its conversation
  "plain_content" TEXT, -- decrypted content
  "send_time" INTEGER,
  "receive_time" INTEGER,
  "display_time" INTEGER,
  "deleted" INTEGER,
  PRIMARY KEY("server_public_key","conversation_id","id"),
  FOREIGN KEY("server_public_key","conversation_id") REFERENCES "Conversation"("server_public_key","id") ON DELETE CASCADE
) WITHOUT ROWID;

CREATE TABLE "Attachment" (
  "server_public_key" BLOB,
  "conversation_id" INTEGER,
  "message_id" INTEGER, -- message's id in its conversation
  "id" INTEGER, -- attachment's id in its message
  "plain_content" BLOB, -- decrypted content
  "type_id" INTEGER,
  PRIMARY KEY("server_public_key","conversation_id","id"),
  FOREIGN KEY("server_public_key","conversation_id") REFERENCES "Conversation"("server_public_key","id") ON DELETE CASCADE
) WITHOUT ROWID;
