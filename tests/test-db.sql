DROP TABLE IF EXISTS "Author";
DROP TABLE IF EXISTS "Book";
DROP TABLE IF EXISTS "Category";
DROP TABLE IF EXISTS "Log";
DROP TABLE IF EXISTS "Spaceship";

CREATE TABLE IF NOT EXISTS "Author" (
    "Id" INTEGER PRIMARY KEY,
    "Name" VARCHAR,
    "BirthYear" INT,
    "Country" VARCHAR,
    "UpdatedAt" DATETIME,
    "Deleted" BOOLEAN
);

CREATE TABLE IF NOT EXISTS "Book" (
    "Id" INTEGER PRIMARY KEY,
    "AuthorId" INTEGER NOT NULL,
    "CoAuthorId" INTEGER,
    "CategoryId" INTEGER,
    "Name" VARCHAR,
    "OriginalName" VARCHAR,
    "ReleaseYear" INT,
    "UpdatedAt" DATETIME,
    "Deleted" BOOLEAN
);

CREATE TABLE IF NOT EXISTS "Category" (
    "Id" INTEGER PRIMARY KEY,
    "Name" VARCHAR,
    "Description" VARCHAR,
    "EditedBy" VARCHAR
);

CREATE TABLE IF NOT EXISTS "Log" (
    "Date" DATETIME NOT NULL,
    "SubjectId" VARCHAR NOT NULL,
    "UserId" INTEGER,
    "Action" VARCHAR,
    PRIMARY KEY ("Date", "SubjectId")
);

CREATE TABLE IF NOT EXISTS "Spaceship" (
    "Id" INTEGER PRIMARY KEY,
    "OwnerId" INTEGER,
    "Name" VARCHAR NOT NULL,
    "BuildYear" INT
);