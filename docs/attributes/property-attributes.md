# Property attributes

## Key

Used on properties that are a primary key

## AutoValue

This attribute must be applied to a property with a `[Key]` attribute. It will try to auto-increment or auto-generate the primary key on insert. Currently only ~integers and~ GUIDs are supported.

## IgnoreOnInsert

When applied to a property, it will be ignored when inserting the object.

## IgnoreOnUpdate

When applied to a property, it will not take this property when executing updates on an entity.

## NotMapped

Properties with this attribute will not be mapped by Extended Dapper.

## UpdatedAt

Can be applied to a DateTime property, and will automatically place the current UTC timestamp on update.

## Deleted

Implements a logical delete for an entity instead of a hard delete. Property must be boolean. (enum support planned in future)

    public abstract class BaseEntity
    {
        [Key]
        [AutoValue]
        public Guid Id { get; set; }
    }

    public abstract class Entity : BaseEntity
    {
        [IgnoreOnUpdate]
        public DateTime CreatedAt { get; set;}

        [UpdatedAt]
        public DateTime? UpdatedAt { get; set; }

        [Deleted]
        public bool Deleted { get; set; }
    }