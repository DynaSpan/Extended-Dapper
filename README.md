# Extended Dapper (WIP)

Based on `MicroOrm.Dapper.Repositories` by @phnx47.

Extends Dapper functionality with a repository pattern, `OneToMany` and `ManyToOne` mappings and searching (on SQL level) using LINQ.

**This is still a WIP and not ready for production use!**

## TODO

- Make sure all queries generate properly (~`SELECT`~, `UPDATE`, `DELETE`, `INSERT`)
- ~Mapping to & from POCOs~ (WORKING: Mapper impl. + default Dapper)
- Make sure `OneToMany` and `ManyToOne` mappings properly apply to ~`INSERT`s~, `UPDATE`s & `SELECT`s (currently implemented for `SELECT`)
- (Proper) support for more than 1 primary key
- Repositories
- Write unittests
- Implement more `SqlProviders`
- Set up CI/CD for automated deployment to NuGet
- Setup documentation
- Transaction support
- Lazy loading of `OneToMany` and `ManyToOne`
- Implement `ManyToMany` attribute