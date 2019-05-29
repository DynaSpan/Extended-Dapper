CREATE TABLE "Author" (
    "Id" GUID PRIMARY KEY,
    "Name" VARCHAR,
    "BirthYear" INT,
    "Country" VARCHAR,
    "UpdatedAt" VARCHAR,
    "Deleted" BOOLEAN
);

CREATE TABLE "Book" (
    "Id" GUID PRIMARY KEY,
    "AuthorId" BLOB,
    "CategoryId" BLOB,
    "Name" VARCHAR,
    "ReleaseYear" INT,
    "UpdatedAt" VARCHAR,
    "Deleted" BOOLEAN
);

CREATE TABLE "Category" (
    "Id" GUID PRIMARY KEY,
    "Name" VARCHAR,
    "Description" VARCHAR
);