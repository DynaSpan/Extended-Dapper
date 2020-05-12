# Property attributes

## Key

Used on properties that are a primary key

## AlternativeKey

Can be used on properties that are used as an alternative primary key. For example; say you have a web API and want to map all internal DB relations with integers, but for external uses you want to use GUIDs. You can create a integer primary key field with a GUID alternative key field.

If the alternative key is filled, but the primary key is not, Extended Dapper will automatically map all entities correctly to their corresponding primary ID. This does require more queries and the use of some reflection, which might have a small impact on performance.

## AutoValue

It will try to auto-increment or auto-generate the primary key on insert if a `[Key]` attribute is also provided. Currently only integers and GUIDs are supported for primary keys.

For the uses on `[AlternativeKey]` and other properties; only GUIDs are currently supported.

## IgnoreOnInsert

When applied to a property, it will be ignored when inserting the object.

## IgnoreOnUpdate

When applied to a property, it will not take this property when executing updates on an entity.

## NotMapped

Properties with this attribute will not be mapped by Extended Dapper.

## UpdatedAt(useUTC = true)

Can be applied to a DateTime property, and will automatically place the current UTC timestamp on update. You can choose to use local time by using `[UpdatedAt(false)]`.

## Deleted

Implements a logical delete for an entity instead of a hard delete. Property must be boolean. (enum support planned in future)

## Example

This is an example of base abstract classes for database entities

    public abstract class BaseEntity
    {
        [Key]
        [AutoValue]
        public int Id { get; set; }
    }

    public abstract class BaseExternalIdEntity : BaseEntity
    {
        [AlternativeKey]
        [AutoValue]
        public Guid ExternalId { get; set; }
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

    public abstract class ExternalIdEntity : Entity, BaseExternalIdEntity
    {
        
    }
