using DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities
{
    public class Meeting
    {
        [Key]
        public long Id { get; set; }
        [Column(TypeName ="varchar(75)")]
        public string Company { get; set; }
        public string MeetingLink { get; set; }
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        public long MeetingOwnerId { get; set; }
        [Column(TypeName = "varchar(75)")]
        public string MeetingOwner { get; set; }
        [Column(TypeName = "varchar(100)")]
        public string MeetingOwnerEmail { get; set; }

        public bool IsCompleted { get; set; }

        public ICollection<MeetingUser> MeetingUsers { get; set; }
        public ICollection<MeetingQuestion> MeetingQuestions { get; set; }
    }

    public enum Language
    {
        english=1,
        swedish = 2
    }

    
    public class Languages 
    {
        [Key]
        public int Id { get; set; }
        public string Language { get; set; }
    }
}

