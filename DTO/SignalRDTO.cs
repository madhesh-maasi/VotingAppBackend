using System;
using System.Collections.Generic;
using System.Text;

namespace DTO
{
    public class UserSessionIputDto
    {
        public long UserId { get; set; }
        public long MeetingId { get; set; }
        public string MeetingLink { get; set; }
        public string SessionBrowserRandomId { get; set; }
    }
    public class UserSession
    {
        public long MeetingId { get; set; }
        public long UserId { get; set; }
        public string ConnectionId { get; set; }
        public string SessionBrowserRandomId { get; set; }
        public string MeetingLink { get; set; }
        public bool isMeetingOwner { get; set; }
        public bool UserCheckin { get; set; }
        public string FirstName { get; set; } 
        public string LastName { get; set; }
    }

    public class MeetingSession
    {
        public long MeetingId { get; set; }
        public int ReleaseQuestions { get; set; }
        public long UserId { get; set; }
    }


    public class releaseQuestionInputDto
    {
        public long meetingId { get; set; }
        public object english { get; set; }
        public object swedish { get; set; }
    }

    public class showresultInputDto
    {
        public long meetingId { get; set; }
        public long questionId { get; set; }
        public object question { get; set; }
        public bool showresult { get; set; }
    }

    public class completepollInputdto
    {
        public long meetingId { get; set; }
        public bool completepoll { get; set; }
    }
}
