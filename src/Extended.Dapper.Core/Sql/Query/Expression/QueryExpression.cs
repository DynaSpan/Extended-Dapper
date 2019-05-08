namespace Extended.Dapper.Core.Sql.Query.Expression
{
    /// <summary>
    /// Abstract Query Expression
    /// </summary>
    internal abstract class QueryExpression
    {
        /// <summary>
        /// Query Expression Node Type
        /// </summary>
        public QueryExpressionType NodeType { get; set; }

        /// <summary>
        /// Operator OR/AND
        /// </summary>
        public string LinkingOperator { get; set; }

        public override string ToString()
        {
            return $"[NodeType:{this.NodeType}, LinkingOperator:{LinkingOperator}]";
        }
    }

    /// <summary>
    /// Query Expression Node Type
    /// </summary>
    internal enum QueryExpressionType
    {
        Parameter = 0,
        Binary = 1,
    }
}