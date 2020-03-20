CREATE TABLE IF NOT EXISTS "LegacyBook" (
    "Id" GUID PRIMARY KEY NOT NULL,
    "AuthorId" GUID NOT NULL,
    "CategoryId" GUID,
    "Name" VARCHAR,
    "ReleaseYear" INT,
    "UpdatedAt" DATETIME,
    "Deleted" BOOLEAN
);