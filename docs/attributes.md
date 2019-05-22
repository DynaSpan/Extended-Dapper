# Extended Dapper Attributes

This document contains documentation about the different attributes Extended Dapper supports, and how to use them.

## Property attributes

### Key

Used on properties that are a primary key

### AutoValue

This attribute must be applied to a property with a `[Key]` attribute. It will try to auto-increment or auto-generate the primary key on insert. Currently only ~integers and~ GUIDs are supported.

### IgnoreOnUpdate

When applied to a property, it will not take this property when executing updates on an entity.

### UpdatedAt

Can be applied to a DateTime property, and will automatically place the current UTC timestamp on update.

### Deleted

Implements a logical delete for an entity instead of a hard delete. Property must be boolean. (enum support planned in future)

## Relation attributes

**Please note:** the parameters in the relation attributes need to be **SQL columns** names, not the property names.

### OneToMany(manyType, foreignKey, localKey = "Id")

Implements a one to many relation on this property, meaning this property be of type `IEnumerable`, `ICollection` or `IList`. Extended Dapper maps all one to manies to a `IList`.

`manyType` should be the type of the many entity, without the `IEnumerable` generic. As a one to many is mapped in the `manyType`'s table, the `foreignKey` parameter should contain the name of the foreign key in this table.

### ManyToOne(oneType, foreignKey, localKey = "Id")

Implements a many to one relation on this property, meaning this property can be of any `object` type. `oneType` should be the type of the property this attribute is applied on. `foreignKey` is the name of the SQL column of this entity's table, which points to the `localKey` of the `oneType`.

### Examples

Since the description above might not be completely clear, an example is added. The Book table contains a field `AuthorId` and `CategoryId`, which contains the primary key of the Author & Category. This field does not have to be mapped seperately in the Book class.

Please note that both classes inherit from `Entity`, thus adding the fields `Id`, `UpdatedAt` and `Deleted`.

    public class Book : Entity
    {
        public string Name { get; set; }

        public int ReleaseYear { get; set; }

        [ManyToOne(typeof(Author), "AuthorId")]
        public Author Author { get; set; }

        [ManyToOne(typeof(Category), "CategoryId")]
        public Category Category { get; set; }
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