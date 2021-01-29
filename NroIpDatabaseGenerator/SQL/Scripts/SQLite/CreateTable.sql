CREATE TABLE @o_tableName (
  "id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  "ip_cidr" TEXT,
  "ip_dec" TEXT,
  "address_count" TEXT NOT NULL,
  "country_code" TEXT(2) DEFAULT XX,
  "country_name" TEXT,
  "update_date" integer,
  "status" TEXT,
  "city" TEXT
);