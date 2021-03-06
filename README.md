# Extended Dapper (preview)

[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Extended.Dapper)](https://www.nuget.org/packages/Extended.Dapper)

Extends Dapper with a repository (CRUD) and native LINQ 2 SQL and `OneToMany` & `ManyToOne` mappings.

**This is a preview and might contain bugs** Though it has been been tested in various (non-critical) production environments and the EntityRepository is almost 100% covered by tests

## Getting started

[View here](docs/getting-started.md)

## Features

- Implements repositories with `CRUD` actions such as `Insert`, `Update`, `Get`, `GetById`, `GetAll` & `Delete` on top of Dapper.
- QueryBuilder for custom SQL select queries
- LINQ queries/searches executed as native SQL queries (no client-side filtering).
- Support for `OneToMany` and `ManyToOne` attributes on entity properties.
    - Choose per `Get(All)` which children you want to include
    - Lazy loading with dedicated methods
- Support for SQLite, MSSQL and MySQL/MariaDB. More planned in the near future.

## Changelog
[View here](CHANGELOG.md)

## Known issues
- CI/CD pipeline not working due to new testing implementation
- None; if you find one; please create an issue :)

## TODO

- Write more unittests (working on it, EntityRepository is covered)
    - Add unittests for non-standard object/SQL formats (e.g. weird pk's)
- ~Setup CI/CD for automated deployment to NuGet~ (need to fix with new testing)
- Setup documentation
- (Proper) support for more than 1 primary key
- ~Support for autovalues other than guid/uuid~
- ~Optimize reflection calls~
- Optimize relation mapping (should be able to better implement it with Dapper)
- Implement more `SqlProviders`
- Lazy loading of `OneToMany` and `ManyToOne` (semi-implemented; loading must be done manually)
- Implement `ManyToMany` attribute
- Do benchmarks

## Example

An example of retrieving an entity from the database:

    var scienceAnswersBook = (await this.BookRepository.GetQueryBuilder()
        .Select(b => b.Name, b => b.ReleaseYear)
        .IncludeChild<Author>(b => b.Author, a => a.Name, a => a.Country)
        .IncludeChild<Author>(b => b.CoAuthor, a => a.Name, a => a.Country)
        .IncludeChild<Category>(b => b.Category, c => c.Name)
        .Where(b => b.Name == "Science questions answered")
        .GetResults()).First();

## Building

Build the library by running `dotnet build -c Release` or `dotnet publish -c Release`.

## Testing

You can run the unittests against the sqlite, MySQL and MSSQL backends with Docker. Issue the following commands: 

    docker-compose build
    docker-compose up --abort-on-container-exit
