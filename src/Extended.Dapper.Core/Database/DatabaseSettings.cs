namespace Extended.Dapper.Core.Database
{
    public class DatabaseSettings
    {
        public string Host { get; set; }

        public int? Port { get; set; }

        public string User { get; set; }

        public string Password { get; set; }

        public string Database { get; set; }

        public DatabaseType ServerEdition { get; set; }

        public bool TrustedConnection { get; set; }
    }

    public enum DatabaseType 
    {
        MSSQL2008,
        MSSQL2012,
        MSSQL2017
    }
}