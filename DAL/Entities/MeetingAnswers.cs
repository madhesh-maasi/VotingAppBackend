using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DAL.Entities
{
    public class MeetingAnswers
    {
        public long Id { get; set;}
        public long MeetingId { get; set; }
        public long UserId { get; set; }
        public long QuestionId { get; set; }
        public string UserAnswer { get; set; }
    }
}
