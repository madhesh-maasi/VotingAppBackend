using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DAL.Entities
{
    public class MeetingQuestion
    {
        [Key]
        public long Id { get; set; }
        [StringLength(250)]
        public string Question { get; set; }
        public string Type { get; set; }
        public int TimeToAnswer { get; set; }
        [StringLength(75)]
        public string Options { get; set; }
        public long QuestionNumber { get; set; }
        public int LanguageId { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public string PostalVotes { get; set; }

        [ForeignKey("Meeting")]
        public long MeetingId { get; set; }

        public Meeting Meeting { get; set; }
    }
}
