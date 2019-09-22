CREATE TABLE "Author" (
    "Id" GUID PRIMARY KEY,
    "Name" VARCHAR,
    "BirthYear" INT,
    "Country" VARCHAR,
    "UpdatedAt" DATETIME,
    "Deleted" BOOLEAN
);

CREATE TABLE "Book" (
    "Id" GUID PRIMARY KEY,
    "AuthorId" GUID,
    "CoAuthorId" GUID,
    "CategoryId" GUID,
    "Name" VARCHAR,
    "OriginalName" VARCHAR,
    "ReleaseYear" INT,
    "UpdatedAt" DATETIME,
    "Deleted" BOOLEAN
);

CREATE TABLE "Category" (
    "Id" GUID PRIMARY KEY,
    "Name" VARCHAR,
    "Description" VARCHAR
);