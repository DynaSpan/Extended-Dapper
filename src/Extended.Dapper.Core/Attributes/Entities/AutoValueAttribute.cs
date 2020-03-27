using System;

namespace Extended.Dapper.Core.Attributes.Entities
{
    /// <summary>
    /// Can be used on a [Key] attribute, implements
    /// an auto-increment/auto-value for a primary key,
    /// or autofills a GUID if it's not a key
    /// </summary>
    public sealed class AutoValueAttribute : Attribute
    {

    }
}