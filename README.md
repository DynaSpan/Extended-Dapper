# Extended Dapper (ALPHA)

Based on `MicroOrm.Dapper.Repositories` by @phnx47.

Extends Dapper functionality with a repository pattern, `OneToMany` and `ManyToOne` mappings and searching (on SQL level) using LINQ.

**This is still a WIP and not ready for production use!**

## Known issues
- OneToMany mapping doesn't work correctly when the includes aren't in the correct order [WIP]

## Changelog
[0.2.9-alpha]
Allow inclusion of multiple children of the same type, which was not possible before and resulted in an exception.

## TODO

- ~Make sure all queries generate properly (`SELECT`, `UPDATE`, `DELETE`, `INSERT`)~
- ~Mapping to & from POCOs~ 
- ~Make sure `OneToMany` and `ManyToOne` mappings properly apply to `INSERT`s, `UPDATE`s, `DELETE`s & `SELECT`s~
- ~Repositories~
- ~Setup local DB for unittests~
- Write unittests (working on it)
- Setup CI/CD for automated deployment to NuGet
- ~Transaction support~
- Setup documentation
- (Proper) support for more than 1 primary key
- Optimize reflection calls
- Optimize relation mapping (should be able to better implement it with Dapper)
- Implement more `SqlProviders` and refactor them
- Lazy loading of `OneToMany` and `ManyToOne`
- Implement `ManyToMany` attribute
- Better error-handling implementation
- Do benchmarks