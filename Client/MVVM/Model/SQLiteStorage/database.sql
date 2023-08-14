DROP TABLE IF EXISTS "Account";
CREATE TABLE "Account" ( -- account on a server
  "login" TEXT,
  "private_key" BLOB,
  PRIMARY KEY("login")
) WITHOUT ROWID;
