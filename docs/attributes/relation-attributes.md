# Relation attributes

**Please note:** the parameters in the relation attributes need to be **SQL columns** names, not the property names.

## OneToMany(manyType, foreignKey, [..])
- `OneToMany(Type manyType, string foreignKey, bool nullabe)`
- `OneToMany(Type manyType string foreignKey, string localKey = "Id", bool nullabe = false)`

Implements a one to many relation on this property, meaning this property be of type `IEnumerable`, `ICollection` or `IList`. Extended Dapper maps all one to manies to a `IList`.

`manyType` should be the type of the many entity, without the `IEnumerable` generic. As a one to many is mapped in the `manyType`'s table, the `foreignKey` parameter should contain the name of the foreign key in this table. The `nullable` toggle indicates if this child is optional.

## ManyToOne(oneType, foreignKey, [..]])
- `ManyToOne(Type oneType, string foreignKey, bool nullabe)`
- `ManyToOne(type oneType, string foreingKey, string localKey = "Id", bool nullable = false)`

Implements a many to one relation on this property, meaning this property can be of any `object` type. `oneType` should be the type of the property this attribute is applied on. `foreignKey` is the name of the SQL column of this entity's table, which points to the `localKey` of the `oneType`. The `nullable` toggle indicates if this child is optional.

## Examples

Since the description above might not be completely clear, an example is added. The Book table contains a field `AuthorId` and `CategoryId`, which contains the primary key of the Author & Category. This field does not have to be mapped seperately in the Book class.

Please note that both classes inherit from `Entity`, thus adding the fields `Id`, `UpdatedAt` and `Deleted`.

    public class Book : Entity
    {
        public string Name { get; set; }

        public int ReleaseYear { get; set; }

        [ManyToOne(typeof(Author), "AuthorId")]
        public Author Author { get; set; }

        [ManyToOne(typeof(Author), "CoAuthorId", true)]
        public Author CoAuthor { get; set; }

        [ManyToOne(typeof(Category), "CategoryId", true)]
        public Category? Category { get; set; }
    }

    public class Author : Entity
    {
        public string Name { get; set; }

        public int BirthYear { get; set; }

        public string Country { get; set; }

        [OneToMany(typeof(Book), "AuthorId")]
        public ICollection<Book> Books { get; set; }
    }

    public class Category : BaseEntity
    {
        [OneToMany(typeof(Book), "CategoryId")]
        public ICollection<Book> Books { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }

    public abstract class BaseEntity
    {
        [Key]
        [AutoValue]
        public Guid Id { get; set; }
    }

    public abstract class Entity : BaseEntity
    {
        [UpdatedAt]
        public DateTime? UpdatedAt { get; set; }

        [Deleted]
        public bool Deleted { get; set; }
    }