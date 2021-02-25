using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Entities
{
    public class MeetingUser
    {
        [Key]
        public long Id { get; set; }
        [Column(TypeName = "varchar(150)")]
        public string FirstName { get; set; }
        [Column(TypeName = "varchar(150)")]
        public string LastName { get; set; }
        [Column(TypeName = "varchar(150)")]
        public string Email { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string MobileNo { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string Language { get; set; }
        public long Voteheld { get; set; }
        public string Link { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string OTP { get; set; }
        public long RefId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public long MeetingId { get; set; }

        public Meeting Meeting { get; set; }
    }
}

