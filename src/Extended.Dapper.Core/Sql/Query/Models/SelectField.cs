namespace Extended.Dapper.Core.Sql.Query.Models
{
    public class SelectField
    {
        public bool IsMainKey { get; set; }

        public string Field { get; set; }

        public string Table { get; set; }

        public string TableAlias { get; set; }

        public string FieldAlias { get; set; }
    }
}