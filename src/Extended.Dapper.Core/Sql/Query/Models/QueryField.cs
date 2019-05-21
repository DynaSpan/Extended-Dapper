namespace Extended.Dapper.Core.Sql.Query.Models
{
    public class QueryField
    {
        public string Table { get; set; }

        public string Field { get; set; }

        public string FieldAlias { get; set; }

        public string ParameterName { get; set; }

        public QueryField(string table, string field, string parameterName = null, string fieldAlias = null)
        {
            this.Table = table;
            this.Field = field;

            if (parameterName == null)
                this.ParameterName = string.Format("@p_{0}", this.Field);
            else
                this.ParameterName = parameterName;

            this.FieldAlias = fieldAlias;
        }
    }
}