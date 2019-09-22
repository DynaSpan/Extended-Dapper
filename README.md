# Extended Dapper (BETA)

Extends Dapper functionality with a repository (CRUD & LINQ), `OneToMany` and `ManyToOne` mappings.

**This is still a WIP and not ready for production use!** Though it has been been tested in various (non-critical) production environments.

## Getting started

[View here](docs/getting-started.md)

## Known issues
- None; if you find one; please create an issue :)

## Changelog
[View here](CHANGELOG.md)

## Features

- Implements repositories with `CRUD` actions such as `Insert`, `Update`, `Get`, `GetAll` & `Delete`.
- Native-SQL searching with LINQ.
- Support for SQLite, MSSQL and MySQL/MariaDB. More planned.
- Support for `OneToMany` and `ManyToOne` attributes on entity properties.

## TODO

- Write unittests (working on it)
- Setup CI/CD for automated deployment to NuGet
- Setup documentation
- (Proper) support for more than 1 primary key
- Optimize reflection calls
- Optimize relation mapping (should be able to better implement it with Dapper)
- Implement more `SqlProviders`
- Lazy loading of `OneToMany` and `ManyToOne` (semi-implemented; loading must be done manually)
- Implement `ManyToMany` attribute
- Do benchmarks