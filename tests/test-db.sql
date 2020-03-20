CREATE TABLE IF NOT EXISTS "Author" (
    "Id" GUID PRIMARY KEY NOT NULL,
    "Name" VARCHAR,
    "BirthYear" INT,
    "Country" VARCHAR,
    "UpdatedAt" DATETIME,
    "Deleted" BOOLEAN
);

CREATE TABLE IF NOT EXISTS "Book" (
    "Id" GUID PRIMARY KEY NOT NULL,
    "AuthorId" GUID NOT NULL,
    "CoAuthorId" GUID,
    "CategoryId" GUID,
    "Name" VARCHAR,
    "OriginalName" VARCHAR,
    "ReleaseYear" INT,
    "UpdatedAt" DATETIME,
    "Deleted" BOOLEAN
);

CREATE TABLE IF NOT EXISTS "Category" (
    "Id" GUID PRIMARY KEY NOT NULL,
    "Name" VARCHAR,
    "Description" VARCHAR,
    "EditedBy" VARCHAR
);

CREATE TABLE IF NOT EXISTS "Log" (
    "Date" DATETIME NOT NULL,
    "SubjectId" VARCHAR NOT NULL,
    "UserId" GUID,
    "Action" VARCHAR,
    PRIMARY KEY ("Date", "SubjectId")
);