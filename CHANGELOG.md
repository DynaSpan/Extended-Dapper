# Changelog

[0.6.1 - 0.6.3]
- Fix diverse bugs regarding updates where the DB doesn't follow normal naming schemes
    - e.g. local (primary) keys have the same name as foreign keys, which differs between tables

[0.6.0]
- Allow AlternativeKeys in case primary key is not available
- Fix some buggies

[0.5.0]
- Add some more tests
- Make BaseEntity and Entity fields virtual
- All the changes of the previews

[0.5.0-preview-8]
- AutoValues don't have to be specifically Keys

[0.5.0-preview-7]
- Change library type back to netstandard2.1
- Allow local time for UpdatedAt property

[0.5.0-preview-6]
**BREAKING CHANGE**: default BaseEntity key converted from GUID to INT
    - In the process also fix some bugs with int keys :)

[0.5.0-preview-5]
- Make Insert & Update return true if updated records are higher than 0 instead of equal to 1
    - If a table has triggers, it could update more records than just the one were updating. Therefore, queries could
    return a failed status when it was actually successfull

[0.5.0-preview-4]
- Fix parameters of EntityRepository.UpdateOnly

[0.5.0-preview-3]
- Allow specific fields to be updated
- Allow specific fields to be selected in the query builder
- Allow specific fields of children to be selected in the query builder

[0.5.0-preview-2]
- Update Mysql.Data to 8.0.19
- Fix bug in MsSql where getting the autoincrement key does not work if the table has a trigger

[0.5.0-preview]
- Add testing for all supported db backends
- Remove/optimize some reflection
- Seperate connection logic from QueryProviders and move them to ConnectionProviders
    - Also refactored some static database info which could lead to problems in runtime when multiple database connections are active
- Update System.Data.SqlClient to 4.8.1
- Add support for integer primary keys with auto increment

[0.4.2-beta]
Add option to force insert an object which has a filled autovalue key.

[0.4.1-beta] 
Add IgnoreOnInsert attribute.

[0.4.0-beta]
First implementation of query builder, which can be used to generate custom select queries. Still requires some work. Also, some refactoring + upgrade to .NET Core 3.1.

[0.3.10-beta]
Fix bug where an update query with multiple children would fail in certain cases.

[0.3.9-beta]
Fix a bug where an object wouldn't get inserted if it had non-autovalue primary keys, that were filled on insert.

[0.3.8-beta]
Fix another bug where updating children didn't always work correctly if the children
were not inserted into the database yet.

[0.3.7-beta]
Fix a bug where inserting an entity with children multiple levels deep didn't work correctly. While doing this also refactored some stuff + unittest for the problem.

[0.3.6-beta]
Fix the GetOne<T> method + add unit tests to cover GetOne<T> and GetMany<T>.

[0.3.5-beta]
Fix some mapping issues

[0.3.3-beta] - [0.3.4-beta]
Fix incorrect exceptions when an exception gets thrown during query execution.

[0.3.2-beta] 
Fix issue where an insert query didn't escape table name.

[0.3.1-beta]
Refactored `SqlQueryProvider` and `SqliteQueryProvider`, thereby also fixing a bug with `Sqlite` updates of entity children.

[0.3.0-beta]
First beta version of Extended.Dapper

- Fix bug where if children weren't alphabetically ordered in the repository get methods, the query would produce incorrect results. This has been fixed.
- SQLite support properly implemented

[0.2.15-alpha] 
SQLite support fixed

[0.2.14-alpha] 
Fix some issues with logical delete & updatedAt

[0.2.13-alpha]
Fix SQLite update queries

[0.2.12-alpha]
Change SQLite param char from '$' to '@'

[0.2.10-alpha]
Allow children as search parameters

[0.2.9-alpha]
Allow inclusion of multiple children of the same type, which was not possible before and resulted in an exception.