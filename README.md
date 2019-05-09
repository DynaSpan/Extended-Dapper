# Extended Dapper (WIP)

Based on `MicroOrm.Dapper.Repositories` by @phnx47.

Extends Dapper functionality with a normal repository pattern, `OneToMany` and `ManyToOne` mappings.

**This is still a WIP and not ready for production use!**

## TODO

- Make sure all queries generate properly (`SELECT`, `UPDATE`, `DELETE`, `INSERT`)
- Mapping to & from POCOs
- Make sure `OneToMany` and `ManyToOne` mappings properly apply to `INSERT`s, `UPDATE`s & `SELECT`s
- Repositories
- Lazy loading of `OneToMany` and `ManyToOne`
- Implement `ManyToMany` attribute
- Write unittests
- Implement more `SqlProviders`
- Set up CI/CD for automated deployment to NuGet