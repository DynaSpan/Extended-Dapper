# Changelog

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