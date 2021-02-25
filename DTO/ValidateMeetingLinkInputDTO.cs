using System;
using System.Collections.Generic;
using System.Text;

namespace DTO
{
    public class ValidateMeetingInputDTO 
    {
        public long MeetingId { get; set; }  
        public long UserId { get; set; }
        public string OTP { get; set;  }
    }

    public class ResponseDTO
    {
        public object Response {    get; set; }  
        public bool IsSuccess  { get; set;}
    }

    public class UserAnswerInputDTO 
    {
        public long MeetingId { get; set; }
        public long QuestionId { get; set; }
        public int LanguageId { get; set; }
        public long UserId { get; set; }
        public string UserAnswer { get; set; }

    }

    public class VoteResponseInputDTO
    {
        public long MeetingId { get; set; }
        public long UserId { get; set; }
        public int LanguageId { get; set; }
    }

    public class VoteResponseDTO
    {
        public long QuestionId { get; set; }
        public string Question { get; set; }
        public string UserAnswer { get; set; }
    }

    public class QuestionInputDTO
    {
        public long MeetingId { get; set; }
        public long QuestionId { get; set; }
        public long LanguageId { get; set; }
    }

    public class SMTPSettings
    {
        public string username { get; set; }
        public string password { get; set; }
        public string smtp_address { get; set; }
        public int port { get; set; }
        public string fromaddress { get; set; }
        public bool isEnableSSL { get; set; }
    }
}
