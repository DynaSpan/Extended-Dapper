# Changelog

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