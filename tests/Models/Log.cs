using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Extended.Dapper.Tests.Models
{
    [Table("Log")]
    public class Log
    {
        [Key]
        public DateTime Date { get; set; }

        [Key]
        public string SubjectId { get; set; }

        public Guid UserId { get; set; }

        public string Action { get; set; }
    }
}