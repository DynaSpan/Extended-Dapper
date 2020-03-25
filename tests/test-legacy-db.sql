CREATE TABLE IF NOT EXISTS "LegacyBook" (
    "Id" GUID PRIMARY KEY NOT NULL,
    "AuthorId" INT NOT NULL,
    "CategoryId" INT,
    "Name" VARCHAR,
    "ReleaseYear" INT,
    "UpdatedAt" DATETIME,
    "Deleted" BOOLEAN
);