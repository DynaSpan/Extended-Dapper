using System;

namespace Extended.Dapper.Core.Attributes.Entities.Keys
{
    /// <summary>
    /// Will mark this property as an alternative key, when the main
    /// primary key is not filled
    /// </summary>
    public sealed class AlternativeKeyAttribute : Attribute
    {
    }
}