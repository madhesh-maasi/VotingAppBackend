using System;
using System.Collections.Generic;
using System.Text;

namespace DTO
{
    public class LoginInputDTO
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string MobileNo { get; set; }
        public string TockenKey { get; set; }
        public string Role { get; set; }
    }

    public class LoginInfo
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        //public string Email { get; set; }
    }

    public class IdValueDto
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }

    public class CompletePollDto
    {
        public long meetingId { get; set; }
        public bool completepoll { get; set; }
    }

    public class ShowResultDto
    {
        public long meetingId { get; set; }
        public long questionId { get; set; }
        public object question { get; set; }
        public bool showresult { get; set; }
    }
}
