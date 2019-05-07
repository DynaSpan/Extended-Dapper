namespace Extended.Dapper.Core.Database
{
    public class DatabaseSettings
    {
        public string Host { get; set; }

        public int? Port { get; set; }

        public string User { get; set; }

        public string Password { get; set; }

        public string Database { get; set; }

        public DatabaseProvider DatabaseProvider { get; set; }

        /// <summary>
        /// Only implemented on SQL SERVER (MSSQL)
        /// </summary>
        public bool? TrustedConnection { get; set; }
    }

    public enum DatabaseProvider 
    {
        MSSQL,
        MySQL
    }
}