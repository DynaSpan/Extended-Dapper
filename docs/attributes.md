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

Please note that the parameters in the relation attributes need to be **SQL columns**, not the property names.

### OneToMany(tableName, localKey, externalKey)

Implements a one 2 many relation on this property, meaning this property be of type `IEnumerable`, `ICollection` or `IList`. Extended Dapper maps all one 2 manies to a `IList`.

`tableName` should contain the name of the table in which the relation is mapped. The `localKey` should reference to the key column/property of the current entity (in their own table), while the `externalKey` references to the foreign key in the `tableName`.

### ManyToOne(tableName, localKey, externalKey)

Implements a many 2 one relation on this property, meaning this property can be of any `object` type. In this case, `tableName` references to the table which contains the data of the property, while `localKey` should be the name of the foreign key in the entity table and `externalKey` the name of the primary key of `tableName`.

### Examples

Since the description above might not be completely clear, an example is added. The Book table contains a field `AuthorId`, which contains the primary key of the Author. This field does not have to be mapped in the Book class.

Please note that both classes inherit from `Entity`, thus adding the fields `Id`, `UpdatedAt` and `Deleted`.

    public class Book : Entity
    {
        public string Name { get; set; }

        public int ReleaseYear { get; set; }

        [ManyToOne("Author", "AuthorId", "Id")]
        public Author Author { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1}) - Author: {2}", Name, ReleaseYear, Author?.Name);
        }
    }

    public class Author : Entity
    {
        public string Name { get; set; }

        public int BirthYear { get; set; }

        public string Country { get; set; }

        [OneToMany("Book", "Id", "AuthorId")]
        public IEnumerable<Book> Books { get; set; }

        public override string ToString()
        {
            var returnString = string.Format("{0} ({1}), {2}", Name, BirthYear, Country);

            if (Books != null)
            {
                foreach (var book in Books)
                {
                    returnString = returnString + Environment.NewLine + " - " + book.Name + " (" + book.ReleaseYear + ")";
                }
            }

            return returnString;
        }
    }