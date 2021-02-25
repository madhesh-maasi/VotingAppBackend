using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO
{
    public class MeetingInputDTO
    {
        public long MeetingId { get; set; }
        public IFormFile UserList { get; set; }
        public IFormFile QuestionList { get; set; }
        public DateTime MeetingDateTime { get; set; }
        public string Notes { get; set; }
        public string Name { get; set; }
        public string MeetingOwner { get; set; }
        public string MeetingOwnerEmail { get; set; }
        public long RefId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CompanyName { get; set; }
        public string MeetingLink { get; set; }
    }

    public class GetMeetingDetailsDTO
    {
        public string meetingAdminId { get; set; }
    }

    public class MeetingOutputDTO
    {
        public long m_id { get; set; }
        public bool m_userExist { get; set; }
        public bool m_questionsExist { get; set; }
        public DateTime m_date { get; set; }
        public string m_notes { get; set; }
        public string m_name { get; set; }

        public string m_owner { get; set; }
        public string m_owner_email { get; set; }

        public bool m_meeting_completed { get; set; }

        public string m_company { get; set; }
        public string m_link { get; set; }
    }

    public class UserListDTO
    {
        public long Id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string language { get; set; }
        public long voteheld { get; set; }
        public string contact_number { get; set; }
    }

    public class UserMeetingLinkDTO : UserListDTO
    {
        public int MeetingId { get; set; }
        public string UniqueId { get; set; }
        public string unique_link { get; set; }
        public string user_name { get; set; }

        //public string meeting_owner { get; set; }

        //public string meeting_owner_email { get; set; }
    }

    public class QuestionListDTO
    {
        public string question_in_english { get; set; } 
        public string question_in_swedish { get; set; }
        public string type { get; set; }
        public int timeToAnswer { get; set; }
        public string options_in_english { get; set; }
        public string options_in_swedish { get; set; }  
        public long qn_no { get; set; }
        public string notes { get; set; }
        public string postal_votes { get; set; }
    }

    public class QuestionResponseDTO
    {
        public long MeetingId { get; set; }
        public string LanguageCode { get; set; }
        public List<QuestionOutputListDTO> Questions { get; set; }
    }

    public class QuestionOutputListDTO
    {
        public object question { get; set; }
        public string type { get; set; }
        public int timeToAnswer { get; set; }
        public string qn_no { get; set; }
        public string notes { get; set; }
        public object optionsarray { get; set; }

        public int LanguageId { get; set; }
        public string Language { get; set; }
    }

    public class QuestionMeetingLinkDTO : QuestionListDTO
    {
        public int MeetingId { get; set; }
        public string UniqueId { get; set; }
    }

    public class QuestionAnswerListDTO
    {
        public long meetingid { get; set; }
        public string question { get; set; }
        public string type { get; set; }
        public object options { get; set; }
        public long qn_no { get; set; }
        public string notes { get; set; }
        public object answer { get; set; }
    }

    public class QuestionAnswerResultDTO
    {
        public long meetingid { get; set; }
        public string question { get; set; }
        public string type { get; set; }
        public object results { get; set; }
        public long qn_no { get; set; }
        public string notes { get; set; }
    }

    public class OptionsDto
    {
        public string English { get; set; }
        public string Swedish { get; set; }
    }

    public class MeetingExportDTO
    {
        public string MeetingID { get; set; }
        public string UserID { get; set; }
        public string QuestionID { get; set; }
        public string Questions { get; set; }
        public string Options { get; set; }
        public string Answers { get; set; }
    }
}
