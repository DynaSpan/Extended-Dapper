# Getting started

## 1. Adding Extended Dapper to your project

[Check the latest version of Extended Dapper on Nuget.org](https://www.nuget.org/packages/Extended.Dapper) and execute the following command to install it:

### .NET CLI

`dotnet add package Extended.Dapper --version [--VERSION--]`

### Package manager

`Install-Package Extended.Dapper -Version [--VERSION--]`

## 2. Creating the database factory

You'll need to provide a connection string or `DatabaseSettings` object to Extended Dapper. Currently, Extended Dapper supports SQLite, MSSQL and MySQL/MariaDB. 

### Providing your own connection string

Construct the DatabaseFactory by creating it: `new DatabaseFactory(string connectionString, DatabaseProvider databaseProvider)`. DatabaseProvider is an enum which contains the values `MSSQL`, `MySQL` and `SQLite`.

### Using the DatabaseSettings object

If you're on ASP.NET (Core), you can use the `appsettings.json` file for configuring your database settings. An example:

    [...],
    "DatabaseConnection": {
        "Host": "localhost",
        "Port": 3306,
        "User": "root",
        "Password": "123456",
        "Database": "mydatabase"
    },
    [...]

which you can cast to a `DatabaseSettings` object in `Startup.cs`: `var dbSettings = Configuration.GetSection("DatabaseConnection").Get<DatabaseSettings>();`. You can then create the DatabaseFactory by `new DatabaseFactory(dbSettings)`. Make sure you provide your DatabaseProvider first: `dbSettings.DatabaseProvider = DatabaseProvider.MySQL;`.

## 3. Using the EntityRepository

After you have created your DatabaseFactory, you can use it to create EntityRepositories. These are repositories that contain most `CRUD` methods suchs as `Insert`, `Update`, `Get`, `GetAll` & `Delete`. In most of these queries, you can use LINQ to search for the objects you want.

The EntityRepository has this signature: `EntityRepository<TEntity>(DatabaseFactory databaseFactory)`. If we have this model:

    public class Category
    {
        [Key]
        [AutoValue]
        public Guid Id { get; set; }

        [OneToMany(typeof(Book), "CategoryId")]
        public ICollection<Book> Books { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }

We can create an EntityRepository `var categoryRepository = new EntityRepository<Category>(dbFactory)`. We can then insert; update; and delete our category when we wish. Most methods return the entity with updated keys and children.

    var scienceCategory = new Category()
    {
        Name = "Science",
        Description = "Cool science books 'n stuff"
    };

    Console.WriteLine(scienceCategory.Id); // Will be Guid.Empty

    var scienceCategoryEntity = await categoryRepository.Insert(scienceCategory);

    Console.WriteLine(scienceCategory.Id); // Will be a random GUID
    Console.WriteLine(scienceCategoryEntity.Id); // Will be a random GUID

    scienceCategoryEntity.Description = "Books about science";

    // You don't have to catch the return value; as it will update
    // the original object as well
    await categoryRepository.Update(scienceCategoryEntity); 

    // Get category with the Books children
    var myCategory = await categoryRepository.Get(c => c.Name == "Science", c => c.Books);

    // With inserts, children get automatically inserted if they dont exist yet (i.e. key is empty)
    var horrorCategory = new Category()
    {
        Name = "Horror"
    };
    var horrorBook = new Book()
    {
        Name = "A Horror Story",
        ReleaseYear = 2010
    };
    horrorCategory.Books.Add(horrorBook);

    var horrorCategoryEntity = await categoryRepository.Insert(horrorCategory);

    Console.WriteLine(horrorBook.Id); // Will be a random GUID

    // With updates, you'll have to include the children you want to update
    // to prevent deletion of children if the child is null
    bool updateResult = await categoryRepository.Update(horrorCategory, c => c.Books);

    // You can also choose to update just one field:
    bool updateFieldResult = await categoryRepository.UpdateOnly(horrorCategory, c => c.Name);

    // You can use the QueryBuilder to create custom queries:
    var categoriesStartingWithA = await categoryRepository.GetQueryBuilder()
        .Select(b => b.Name) // which fields of the entity you want to include
        .IncludeChild<Book>(c => c.Books) // which children you want to include
        .Where(c => c.Name.StartsWith("A")) // search
        .OrderBy(c => c.Name, OrderBy.ASC)
        .GetResults();

// TODO: add more here