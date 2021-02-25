using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BAL.Helpers;
using CsvHelper;
using DAL;
using DAL.Context;
using DAL.Entities;
using DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BAL
{
    public interface IMeetingServices
    {
        public MeetingOutputDTO AddMeeting(MeetingInputDTO meetingDetails, string baseURL, string MeetingOwnerContent);
        public MeetingOutputDTO UpdateMeeting(MeetingInputDTO meetingDetails);
        List<MeetingOutputDTO> GetAllMeeting(long LoginUserId);
        public MeetingOutputDTO GetMeeting(long meetingID);
        public List<UserMeetingLinkDTO> GetAllMeetingUsers(long meetingID);
        public List<QuestionResponseDTO> GetAllMeetingQuestions(long meetingID);
        public ResponseDTO ValidateMeetingLink(string meetingLink, string mailbody);
        public MeetingUser ValidateOTP(ValidateMeetingInputDTO inputDTO);
        public ResponseDTO StoreUserPollAnswer(UserAnswerInputDTO dto);
        public object GetVoteResponse();
        public List<QuestionAnswerListDTO> GetUserQuestionAnswer(VoteResponseInputDTO inputDTO);
        public bool SendSummaryReort(long userId);
        public DataTable DownloadReport(long meetingId, int languageId);
        public List<QuestionAnswerResultDTO> GetMeetingResult(VoteResponseInputDTO inputDTO);
        public QuestionAnswerResultDTO GetMeetingQuestionResult(QuestionInputDTO inputDTO);
        public bool SendMeetingRequestEmail(long meetingId, string baseURL, string mailContent);


        //SignalR

        public CompletePollDto CompletePoll(CompletePollDto dto);
    }

    public class MeetingServices : IMeetingServices
    {
        private readonly IAppDBContext db;
        private readonly IOptions<SMTPSettings> _SMTPSettings;
        public MeetingServices(IAppDBContext dbContext, IOptions<SMTPSettings> SMTPSettings)
        {
            db = dbContext;
            _SMTPSettings = SMTPSettings;
        }

        public CompletePollDto CompletePoll(CompletePollDto dto)
        {
            try
            {
                var meeting = db.Meetings.FirstOrDefault(s => s.Id == dto.meetingId);
                meeting.IsCompleted = true;
                db.SaveChanges();
                dto.completepoll = true;
                return dto;
            }
            catch (Exception ex)
            {
                dto.completepoll = false;
                return dto;
            }
        }

        public bool SendMeetingRequestEmail(long meetingId, string baseURL, string mailContent)
        {
            try
            {
                var meeting = db.Meetings.FirstOrDefault(s => s.Id == meetingId);
                var users = db.MeetingUsers.Where(s => s.MeetingId == meetingId).ToList();
                foreach (var item in users)
                {
                    string content = mailContent;
                    string URL = baseURL + item.Link;
                    content = content.Replace("{First_name}", item.FirstName);
                    content = content.Replace("{URL}", URL);
                    content = content.Replace("{date}", meeting.DateTime.ToString());
                    content = content.Replace("{Company_name}", meeting.Company);
                    SendMail($"Link to the general meeting of { meeting.Company }", content, item.Email);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<VoteResponseDTO> GetVoteResponse(VoteResponseInputDTO dto)
        {
            try
            {
                var res = (from meeting in db.Meetings
                           join questions in db.MeetingQuestions.AsNoTracking() on meeting.Id equals questions.Meeting.Id
                           join answers in db.MeetingAnswers.AsNoTracking() on questions.Meeting.Id equals answers.MeetingId
                           where answers.MeetingId == dto.MeetingId && answers.UserId == dto.UserId
                           select new
                           {
                               meeting,
                               questions,
                               answers
                           }).Select((r, i) => new VoteResponseDTO
                           {
                               QuestionId = r.questions.Id,
                               Question = r.questions.Question,
                               UserAnswer = r.answers.UserAnswer
                           }).ToList();
                return res;
            }
            catch (Exception ex)
            {
                return new List<VoteResponseDTO>();
            }
        }

        public ResponseDTO StoreUserPollAnswer(UserAnswerInputDTO dto)
        {
            try
            {
                var ans = db.MeetingQuestions.Where(s => s.MeetingId == dto.MeetingId && s.QuestionNumber == dto.QuestionId).ToList();
                var available_options = ans.FirstOrDefault(s => s.LanguageId == dto.LanguageId).Options.Split(':');

                var selected_index = available_options.ToList().IndexOf(dto.UserAnswer);

                var available_options_in_english = ans.FirstOrDefault(s => s.LanguageId == (int)Language.english).Options.Split(':');

                var user_answer_in_english = available_options_in_english[selected_index];

                var meetingUserAnswer = db.MeetingAnswers.FirstOrDefault(s => s.MeetingId == dto.MeetingId && s.UserId == dto.UserId && s.QuestionId == dto.QuestionId);
                if (meetingUserAnswer == null)
                {
                    var meetingAnswer = new MeetingAnswers()
                    {
                        MeetingId = dto.MeetingId,
                        UserId = dto.UserId,
                        QuestionId = dto.QuestionId,
                        UserAnswer = user_answer_in_english
                    };
                    db.MeetingAnswers.Add(meetingAnswer);
                    db.SaveChanges();
                }
                else
                {
                    meetingUserAnswer.UserAnswer = user_answer_in_english;
                    db.SaveChanges();
                }
                return new ResponseDTO()
                {
                    Response = "user answer saved successfully",
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new ResponseDTO()
                {
                    Response = "user answer save failed",
                    IsSuccess = true
                };
            }
        }

        public string GetPropValue(String name, Object obj)
        {
            foreach (String part in name.Split('.'))
            {
                if (obj == null) { return null; }

                Type type = obj.GetType();
                PropertyInfo info = type.GetProperty(part);
                if (info == null) { return null; }

                obj = info.GetValue(obj, null);
            }
            return Convert.ToString(obj);
        }

        public MeetingOutputDTO AddMeeting(MeetingInputDTO meetingDetails, string baseURL, string MeetingOwnerContent)
        {
            Random r = new Random();
            int genRand = r.Next(1000000, 9999999);
            var meetingOwner = new Users()
            {
                Username = meetingDetails.MeetingOwner,
                Email = meetingDetails.MeetingOwnerEmail,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                Role = "MeetingOwner",
                Password = genRand.ToString()
            };

            string mailContent = MeetingOwnerContent;
            mailContent = mailContent.Replace("{First_name}", meetingDetails.MeetingOwner);
            mailContent = mailContent.Replace("{OTP}", genRand.ToString());
            SendMail("Meeting Admin Login Credenticals", mailContent, meetingDetails.MeetingOwnerEmail);

            db.Users.Add(meetingOwner);
            db.SaveChanges();

            Meeting meet = new Meeting();
            meet.MeetingOwnerId = meetingOwner.Id;
            meet.MeetingOwner = meetingDetails.MeetingOwner;
            meet.MeetingOwnerEmail = meetingDetails.MeetingOwnerEmail;
            meet.DateTime = meetingDetails.MeetingDateTime;
            meet.Notes = meetingDetails.Notes;
            meet.CreatedDate = DateTime.Now;
            meet.IsActive = true;
            meet.Name = meetingDetails.Name;
            meet.Company = meetingDetails.CompanyName;
            meet.MeetingLink = meetingDetails.MeetingLink;
            db.Meetings.Add(meet);

            if (meetingDetails.QuestionList != null && meetingDetails.QuestionList.Length > 0)
            {
                var questionstream = meetingDetails.QuestionList.OpenReadStream();
                TextReader questionreader = new StreamReader(questionstream);
                var questioncsvReader = new CsvReader(questionreader);
                var questionrecords = questioncsvReader.GetRecords<DTO.QuestionListDTO>();

                var lst = new List<IdValueDto>()
                {
                    new IdValueDto(){ Id=(int)Language.english,Value=Language.english.ToString()},
                    new IdValueDto(){ Id=(int)Language.swedish,Value=Language.swedish.ToString()}
                };

                foreach (var item in questionrecords)
                {
                    foreach (var lng in lst)
                    {
                        var ques = "question_in_" + lng.Value;
                        var option = "options_in_" + lng.Value;
                        MeetingQuestion question = new MeetingQuestion();
                        question.Question = GetPropValue(ques, item);
                        question.Type = item.type;
                        question.TimeToAnswer = item.timeToAnswer;
                        question.Options = GetPropValue(option, item);
                        question.QuestionNumber = item.qn_no;
                        question.Notes = item.notes;
                        question.CreatedDate = DateTime.Now;
                        question.Meeting = meet;
                        question.LanguageId = lng.Id;
                        question.PostalVotes = item.postal_votes;
                        db.MeetingQuestions.Add(question);
                    }
                }

            }

            if (meetingDetails.UserList != null && meetingDetails.UserList.Length > 0)
            {
                var userstream = meetingDetails.UserList.OpenReadStream();
                TextReader userreader = new StreamReader(userstream);
                var usercsvReader = new CsvReader(userreader);
                var userrecords = usercsvReader.GetRecords<DTO.UserListDTO>();

                foreach (var item in userrecords)
                {
                    var processedRecords = new List<string>();
                    GenerateGUID:
                    var guid = Guid.NewGuid().ToString();
                    if (processedRecords.Any(s => s == guid))
                    {
                        goto GenerateGUID;
                    }
                    string unique_link = string.Concat(meet.Id, "+", guid).ToBase64EncodeString();
                    processedRecords.Add(guid);
                    MeetingUser users = new MeetingUser();
                    users.Email = item.email;
                    users.FirstName = item.first_name;
                    users.Language = item.language;
                    users.LastName = item.last_name;
                    users.MobileNo = item.contact_number;
                    users.RefId = item.Id;
                    users.Link = unique_link;
                    users.Voteheld = item.voteheld;
                    users.CreatedDate = DateTime.Now;
                    users.Meeting = meet;

                    db.MeetingUsers.Add(users);
                }
            }

            db.SaveChanges();

            if (meetingDetails.UserList != null && meetingDetails.UserList.Length > 0)
            {
                //var users = db.MeetingUsers.Where(s => s.MeetingId == meet.Id).ToList();
                //foreach (var item in users)
                //{
                //    string URL = baseURL + item.Link;
                //    string invitationContent = invitationOwnerContent; 
                //    invitationContent = mailContent.Replace("{First_name}", item.FirstName);
                //    invitationContent = mailContent.Replace("{URL}", URL);
                //    invitationContent = mailContent.Replace("{date}", meet.DateTime.ToString());
                //    SendMail("Link to the general meeting of {Company_name}", invitationContent, item.Email);
                //}
            }

            MeetingOutputDTO meetinDet = new MeetingOutputDTO() { m_date = meet.DateTime, m_id = meet.Id, m_name = meet.Name, m_notes = meet.Notes, m_questionsExist = meetingDetails.QuestionList != null ? true : false, m_userExist = meetingDetails.UserList != null ? true : false, m_company = meet.Company, m_meeting_completed = meet.IsCompleted, m_owner_email = meet.MeetingOwnerEmail, m_owner = meet.MeetingOwner, m_link = meetingDetails.MeetingLink };

            return meetinDet;
        }

        public MeetingOutputDTO UpdateMeeting(MeetingInputDTO meetingDetails)
        {
            var meet = db.Meetings.FirstOrDefault(s => s.Id == meetingDetails.MeetingId);
            MeetingOutputDTO meetinDet = new MeetingOutputDTO();

            if (meet != null)
            {
                meet.DateTime = meetingDetails.MeetingDateTime;
                meet.Notes = meetingDetails.Notes;
                meet.CreatedDate = DateTime.Now;
                meet.IsActive = true;
                meet.Name = meetingDetails.Name;
                meet.Company = meetingDetails.CompanyName;
                meet.MeetingLink = meetingDetails.MeetingLink;

                if (meetingDetails.UserList != null && meetingDetails.UserList.Length > 0)
                {
                    var existinguserrecords = db.MeetingUsers.Where(s => s.MeetingId == meetingDetails.MeetingId);
                    db.MeetingUsers.RemoveRange(existinguserrecords);
                    //foreach (var item in existinguserrecords)
                    //{
                    //    db.MeetingUsers.Remove(item);
                    //}
                    db.SaveChanges();
                    var userstream = meetingDetails.UserList.OpenReadStream();
                    TextReader userreader = new StreamReader(userstream);
                    var usercsvReader = new CsvReader(userreader);
                    var userrecords = usercsvReader.GetRecords<DTO.UserListDTO>();

                    foreach (var item in userrecords)
                    {
                        var processedRecords = new List<string>();
                        GenerateGUID:
                        var guid = Guid.NewGuid().ToString();
                        if (processedRecords.Any(s => s == guid))
                        {
                            goto GenerateGUID;
                        }
                        string unique_link = string.Concat(meet.Id, "+", guid).ToBase64EncodeString();
                        processedRecords.Add(guid);
                        MeetingUser users = new MeetingUser();
                        users.Email = item.email;
                        users.FirstName = item.first_name;
                        users.Language = item.language;
                        users.LastName = item.last_name;
                        users.MobileNo = item.contact_number;
                        users.RefId = item.Id;
                        users.Link = unique_link;
                        users.Voteheld = item.voteheld;
                        users.CreatedDate = DateTime.Now;
                        users.Meeting = meet;

                        db.MeetingUsers.Add(users);
                    }
                }

                if (meetingDetails.QuestionList != null && meetingDetails.QuestionList.Length > 0)
                {
                    var existingquestionrecords = db.MeetingQuestions.Where(s => s.MeetingId == meetingDetails.MeetingId);
                    db.MeetingQuestions.RemoveRange(existingquestionrecords);

                    //foreach (var item in existingquestionrecords)
                    //{
                    //    db.MeetingQuestions.Remove(item);
                    //}
                    db.SaveChanges();

                    var questionstream = meetingDetails.QuestionList.OpenReadStream();
                    TextReader questionreader = new StreamReader(questionstream);
                    var questioncsvReader = new CsvReader(questionreader);
                    var questionrecords = questioncsvReader.GetRecords<DTO.QuestionListDTO>();
                    var lst = new List<IdValueDto>()
                {
                    new IdValueDto(){ Id=(int)Language.english,Value=Language.english.ToString()},
                    new IdValueDto(){ Id=(int)Language.swedish,Value=Language.swedish.ToString()}
                };

                    foreach (var item in questionrecords)
                    {
                        foreach (var lng in lst)
                        {
                            var ques = "question_in_" + lng.Value;
                            var option = "options_in_" + lng.Value;

                            MeetingQuestion question = new MeetingQuestion();
                            question.Question = GetPropValue(option, item);
                            question.Type = item.type;
                            question.TimeToAnswer = item.timeToAnswer;
                            question.Options = GetPropValue(ques, item);
                            question.QuestionNumber = item.qn_no;
                            question.Notes = item.notes;
                            question.CreatedDate = DateTime.Now;
                            question.Meeting = meet;
                            question.PostalVotes = item.postal_votes;
                            question.LanguageId = lng.Id;
                            db.MeetingQuestions.Add(question);
                        }
                    }
                }
                db.SaveChanges();

                meetinDet = new MeetingOutputDTO()
                {
                    m_date = meet.DateTime,
                    m_id = meet.Id,
                    m_name = meet.Name,
                    m_notes = meet.Notes,
                    m_questionsExist = meetingDetails.QuestionList != null ? true : false,
                    m_userExist = meetingDetails.UserList != null ? true : false,
                    m_company = meet.Company,
                    m_meeting_completed = meet.IsCompleted,
                    m_owner = meet.MeetingOwner,
                    m_owner_email = meet.MeetingOwnerEmail,
                    m_link = meetingDetails.MeetingLink
                };
            }
            return meetinDet;
        }

        public MeetingOutputDTO GetMeeting(long meetingID)
        {
            Meeting meet = db.Meetings.Include("MeetingUsers").Include("MeetingQuestions").FirstOrDefault(s => s.Id == meetingID);
            MeetingOutputDTO meetinDet = new MeetingOutputDTO() { m_date = meet.DateTime, m_id = meet.Id, m_name = meet.Name, m_notes = meet.Notes, m_questionsExist = meet.MeetingQuestions != null && meet.MeetingQuestions.Count > 0 ? true : false, m_userExist = meet.MeetingUsers != null && meet.MeetingUsers.Count > 0 ? true : false, m_owner = meet.MeetingOwner, m_owner_email = meet.MeetingOwnerEmail, m_meeting_completed = meet.IsCompleted, m_company = meet.Company, m_link = meet.MeetingLink };

            return meetinDet;
        }

        public List<MeetingOutputDTO> GetAllMeeting(long LoginUserId)
        {
            var user = db.Users.FirstOrDefault(s => s.Id == LoginUserId);
            if (user.Role != "MeetingOwner")
            {
                var meetings = db.Meetings.Include("MeetingUsers").Include("MeetingQuestions").ToList();
                List<MeetingOutputDTO> lst = new List<MeetingOutputDTO>();
                foreach (var meet in meetings)
                {
                    MeetingOutputDTO meetinDet = new MeetingOutputDTO()
                    {
                        m_date = meet.DateTime,
                        m_id = meet.Id,
                        m_name = meet.Name,
                        m_notes = meet.Notes,
                        m_questionsExist = meet.MeetingQuestions != null && meet.MeetingQuestions.Count > 0 ? true : false,
                        m_userExist = meet.MeetingUsers != null && meet.MeetingUsers.Count > 0 ? true : false,
                        m_meeting_completed = meet.IsCompleted,
                        m_link = meet.MeetingLink,
                        m_company = meet.Company,
                        m_owner = meet.MeetingOwner,
                        m_owner_email = meet.MeetingOwnerEmail

                    };

                    lst.Add(meetinDet);
                }

                return lst.OrderByDescending(s => s.m_date).ToList();
            }
            else
            {
                var meetings = db.Meetings.Include("MeetingUsers").Include("MeetingQuestions").Where(s => s.MeetingOwnerId == LoginUserId).ToList();
                List<MeetingOutputDTO> lst = new List<MeetingOutputDTO>();
                foreach (var meet in meetings)
                {
                    MeetingOutputDTO meetinDet = new MeetingOutputDTO()
                    {
                        m_date = meet.DateTime,
                        m_id = meet.Id,
                        m_name = meet.Name,
                        m_notes = meet.Notes,
                        m_questionsExist = meet.MeetingQuestions != null && meet.MeetingQuestions.Count > 0 ? true : false,
                        m_userExist = meet.MeetingUsers != null && meet.MeetingUsers.Count > 0 ? true : false,
                        m_company = meet.Company,
                        m_meeting_completed = meet.IsCompleted,
                        m_owner = meet.MeetingOwner,
                        m_owner_email = meet.MeetingOwnerEmail
                    };

                    lst.Add(meetinDet);
                }

                return lst.OrderByDescending(s => s.m_id).ToList();
            }
        }

        public List<UserMeetingLinkDTO> GetAllMeetingUsers(long meetingID)
        {
            var meetings = db.MeetingUsers.Where(s => s.MeetingId == meetingID).ToList();
            List<UserMeetingLinkDTO> lst = new List<UserMeetingLinkDTO>();
            foreach (var meet in meetings)
            {
                UserMeetingLinkDTO meetinDet = new UserMeetingLinkDTO() { contact_number = meet.MobileNo, email = meet.Email, first_name = meet.FirstName, Id = meet.Id, language = meet.Language, last_name = meet.LastName, voteheld = meet.Voteheld, user_name = meet.FirstName + "_" + meet.LastName, unique_link = meet.Link };

                lst.Add(meetinDet);
            }

            return lst;
        }

        public List<QuestionResponseDTO> GetAllMeetingQuestions(long meetingID)
        {
            var meetings = db.MeetingQuestions.Where(s => s.MeetingId == meetingID).ToList();
            List<QuestionOutputListDTO> lst = new List<QuestionOutputListDTO>();
            var lstLangeuage = new List<IdValueDto>()
                {
                    new IdValueDto(){ Id=(int)Language.english,Value=Language.english.ToString()},
                    new IdValueDto(){ Id=(int)Language.swedish,Value=Language.swedish.ToString()}
                };


            foreach (var meet in meetings)
            {
                string[] language_options = meet.Options.Split(':');
                //string[] spanish_options = meet.Options_Spanish.Split(':');

                QuestionOutputListDTO meetinDet = new QuestionOutputListDTO()
                {
                    notes = meet.Notes,
                    optionsarray = language_options,
                    qn_no = meet.QuestionNumber.ToString(),
                    question = meet.Question,
                    timeToAnswer = meet.TimeToAnswer,
                    type = meet.Type,
                    LanguageId = meet.LanguageId

                };

                lst.Add(meetinDet);
            }

            var res = lst.Join(lstLangeuage, a => a.LanguageId, b => b.Id, (a, b) => new { a, b.Value }).GroupBy(s => s.Value).Select(s => new QuestionResponseDTO() { LanguageCode = s.Key, Questions = s.Select(s => s.a).ToList() }).ToList();
            //var r = res.FirstOrDefault(s => s.LanguageCode == "english").Questions;
            return res;
        }

        private void AddMailAddresses(MailAddressCollection addressCollection, string emailIDs)
        {
            if (!string.IsNullOrEmpty(emailIDs))
            {
                emailIDs = emailIDs.Trim();

                var idCollection = emailIDs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var id in idCollection)
                {
                    string newID = id.Trim();

                    if (!string.IsNullOrEmpty(newID))
                    {
                        addressCollection.Add(newID);
                    }
                }
            }
        }

        public ResponseDTO ValidateMeetingLink(string meetingLink, string mailbody)
        {
            var userInfo = db.MeetingUsers.FirstOrDefault(s => s.Link.Trim() == meetingLink.Trim());
            if (userInfo == null)
            {
                return new ResponseDTO()
                {
                    Response = "user link is not valid",
                    IsSuccess = false

                };
            }
            return SendOTP(userInfo, mailbody);
        }

        public T GetEnumValue<T>(string str) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new Exception("T must be an Enumeration type.");
            }
            T val = ((T[])Enum.GetValues(typeof(T)))[0];
            if (!string.IsNullOrEmpty(str))
            {
                foreach (T enumValue in (T[])Enum.GetValues(typeof(T)))
                {
                    if (enumValue.ToString().ToUpper().Equals(str.ToUpper()))
                    {
                        val = enumValue;
                        break;
                    }
                }
            }

            return val;
        }
        public ResponseDTO SendOTP(MeetingUser userInfo, string mailbody)
        {
            try
            {
                Random r = new Random();
                int genRand = r.Next(100000, 999999);
                var meeting = db.Meetings.FirstOrDefault(s => s.Id == userInfo.MeetingId);
                string mailContent = mailbody;
                mailContent = mailContent.Replace("{First_name}", userInfo.FirstName);
                mailContent = mailContent.Replace("{OTP}", genRand.ToString());
                mailContent = mailContent.Replace("{Company_name}", meeting.Company);
                mailContent = mailContent.Replace("{date}", meeting.DateTime.ToString());

                SendMail($"OTP to the general meeting of {meeting.Company}", mailContent, userInfo.Email);

                userInfo.OTP = genRand.ToString();
                db.SaveChanges();
                var lng = GetEnumValue<Language>(userInfo.Language.ToLower());
                return new ResponseDTO()
                {

                    Response = new
                    {
                        UserId = userInfo.Id,
                        MeetingId = userInfo.MeetingId,
                        UserLanguage = userInfo.Language.ToLower(),
                        LanguageId = ((int)lng),
                        Company = meeting.Company,
                        MeetingDateTime = meeting.DateTime,
                        GentralMeetingLink = meeting.MeetingLink
                    },
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new ResponseDTO()
                {
                    Response = "OTP send failed(Please contact admin)",
                    IsSuccess = false
                };

            }
        }

        public void SendMail(string subject, string body, string emailID)
        {
            MailMessage message = new MailMessage();
            message.From = new MailAddress(_SMTPSettings.Value.fromaddress);
            AddMailAddresses(message.To, emailID);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient(_SMTPSettings.Value.smtp_address, _SMTPSettings.Value.port);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.UseDefaultCredentials = true;
            smtp.Credentials = new NetworkCredential(_SMTPSettings.Value.username, _SMTPSettings.Value.password);
            smtp.EnableSsl = _SMTPSettings.Value.isEnableSSL;
            smtp.Timeout = 600000;
            smtp.Send(message);

            //MailMessage message = new MailMessage();
            //message.From = new MailAddress("umasankar.gv@gmail.com");
            //AddMailAddresses(message.To, emailID);
            //message.Subject = subject;
            //message.Body = body;
            //message.IsBodyHtml = true;
            //SmtpClient smtp = new SmtpClient("email-smtp.us-east-2.amazonaws.com", 587);
            //smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            //smtp.UseDefaultCredentials = true;
            //smtp.Credentials = new NetworkCredential("AKIAS7E3VU5QAZ7QYMTT", "BFeS9jTVDYoqBIUAGfC8qbrOqIINCfC35G1oCN5uUg3y");
            //smtp.EnableSsl = true;
            //smtp.Timeout = 600000;
            //smtp.Send(message);
        }

        public MeetingUser ValidateOTP(ValidateMeetingInputDTO inputDTO)
        {
            var user = db.MeetingUsers.FirstOrDefault(s => s.Id == inputDTO.UserId && s.Meeting.Id == inputDTO.MeetingId);
            if (user.OTP == inputDTO.OTP)
            {
                return user;
            }
            else
            {
                return null;
            }
        }

        public bool OTPVerification(string meetingLink, string otp)
        {
            var userInfo = db.MeetingUsers.FirstOrDefault(s => s.Link.Trim() == meetingLink.Trim() && s.OTP == otp);
            bool isValid = true;
            if (userInfo == null)
                isValid = false;

            return isValid;
        }

        public object GetVoteResponse()
        {
            throw new NotImplementedException();
        }

        public List<QuestionAnswerListDTO> GetUserQuestionAnswer(VoteResponseInputDTO dto)
        {

            //var ans = db.MeetingQuestions.Where(s => s.MeetingId == dto.MeetingId && dto.QuestionId == dto.QuestionId).ToList();
            //var available_options = ans.FirstOrDefault(s => s.LanguageId == dto.LanguageId).Options.Split(':');

            //var selected_index = available_options.ToList().IndexOf(dto.UserAnswer);

            var questions = db.MeetingQuestions.Where(s => s.Meeting.Id == dto.MeetingId).ToList().GroupBy(s => s.QuestionNumber);
            var userAns = db.MeetingAnswers.Where(s => s.MeetingId == dto.MeetingId && s.UserId == dto.UserId).ToList();
            List<QuestionAnswerListDTO> qusAns = new List<QuestionAnswerListDTO>();

            foreach (var question in questions)
            {
                var user_question_Answer = question.FirstOrDefault(s => s.LanguageId == (int)Language.english);
                var user_question_selected_language = question.FirstOrDefault(s => s.LanguageId == dto.LanguageId);
                var available_options_english = question.FirstOrDefault(s => s.LanguageId == (int)Language.english).Options.Split(':');

                var available_options_selectedLanguage = question.FirstOrDefault(s => s.LanguageId == dto.LanguageId).Options.Split(':');

                //var selected_index = available_options.ToList().IndexOf(dto.UserAnswer);

                // var lng_questions = question.Select(s=>s.Options); 
                QuestionAnswerListDTO qusAnsw = new QuestionAnswerListDTO();
                //var spanishOptions = question.Options_Spanish.Split(":").ToList(); 
                //  var language_Options = available_options_english.Split(":").ToList();

                if (userAns.Count > 0)
                {
                    var val = userAns.FirstOrDefault(s => s.QuestionId == user_question_Answer.QuestionNumber);
                    if (val != null)
                    {
                        if (available_options_english.Contains(val.UserAnswer))
                        {
                            var position = available_options_english.ToList().IndexOf(val.UserAnswer);
                            qusAnsw.answer = available_options_selectedLanguage[position];
                        }
                    }
                }
                var opt = question.FirstOrDefault(s => s.LanguageId == dto.LanguageId).Options.Split(':');
                qusAnsw.meetingid = user_question_Answer.MeetingId;
                qusAnsw.type = user_question_Answer.Type;
                qusAnsw.question = user_question_selected_language.Question;
                qusAnsw.qn_no = user_question_Answer.QuestionNumber;
                qusAnsw.options = opt;
                qusAnsw.notes = user_question_Answer.Notes;
                qusAns.Add(qusAnsw);
            }
            return qusAns;
        }

        public bool SendSummaryReort(long userId)
        {
            var user = db.MeetingUsers.FirstOrDefault(s => s.Id == userId);
            var meeting = db.Meetings.FirstOrDefault(s => s.Id == user.MeetingId);

            var questions = db.MeetingQuestions.Where(s => s.Meeting.Id == user.MeetingId).ToList().GroupBy(s => s.QuestionNumber);
            var userAns = db.MeetingAnswers.Where(s => s.MeetingId == user.MeetingId && s.UserId == userId).ToList();
            List<QuestionAnswerListDTO> qusAns = new List<QuestionAnswerListDTO>();
            var lngId = user.Language.ToLower() == "english" ? 1 : 2;
            var str = new StringBuilder();
            //str.Append(@"<table><tr><td style='text-indent:15px'>Dear " + user.FirstName + " " + user.LastName + "</td></tr><tr><td></td></tr><tr><td style='margin-bottom:25px;'></td></tr><table>");

            str.Append("<p>Dear "+ user.FirstName +" "+ user.LastName+"</br></p>");
            str.Append("<p style='text-indent:10px;margin=bottom25px;'>you can saw the summary result of the meeting <b>" + " " + meeting.Name + "</b></p>");

            str.Append("<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"min-width:100%;\">");
            str.Append("<thead>");
            str.Append("<tr>");
            str.Append("<th scope='col' style='padding:5px; font-family: Arial,sans-serif; font-size: 16px; line-height:20px;line-height:30px;border: 1px solid #ccc;text-align:center;'>Question</th>");
            str.Append("<th scope='col' style='padding:5px; font-family: Arial,sans-serif; font-size: 16px; line-height:20px;line-height:30px;border: 1px solid #ccc;text-align:center;'>Options</th>");
            str.Append("<th scope='col' style='padding:5px; font-family: Arial,sans-serif; font-size: 16px; line-height:20px;line-height:30px;border: 1px solid #ccc;text-align:center;'>Your Answer</th>");
            str.Append("</tr>");
            str.Append("    </thead>");
            str.Append("<tbody>");
            foreach (var question in questions)
            {
                var user_question_Answer = question.FirstOrDefault(s => s.LanguageId == (int)Language.english);
                var user_question_selected_language = question.FirstOrDefault(s => s.LanguageId == lngId);
                var available_options_english = question.FirstOrDefault(s => s.LanguageId == (int)Language.english).Options.Split(':');

                var available_options_selectedLanguage = question.FirstOrDefault(s => s.LanguageId == lngId).Options.Split(':');

                //var selected_index = available_options.ToList().IndexOf(dto.UserAnswer);

                // var lng_questions = question.Select(s=>s.Options); 
                QuestionAnswerListDTO qusAnsw = new QuestionAnswerListDTO();
                //var spanishOptions = question.Options_Spanish.Split(":").ToList(); 
                //  var language_Options = available_options_english.Split(":").ToList();

                if (userAns.Count > 0)
                {
                    var val = userAns.FirstOrDefault(s => s.QuestionId == user_question_Answer.QuestionNumber);
                    if (val != null)
                    {
                        if (available_options_english.Contains(val.UserAnswer))
                        {
                            var position = available_options_english.ToList().IndexOf(val.UserAnswer);
                            qusAnsw.answer = available_options_selectedLanguage[position];
                        }
                    }
                }

                var opt = question.FirstOrDefault(s => s.LanguageId == lngId).Options.Split(':');
                qusAnsw.meetingid = user_question_Answer.MeetingId;
                qusAnsw.type = user_question_Answer.Type;
                qusAnsw.question = user_question_selected_language.Question;
                qusAnsw.qn_no = user_question_Answer.QuestionNumber;
                qusAnsw.options = opt;
                qusAnsw.notes = user_question_Answer.Notes;
                qusAns.Add(qusAnsw);

                var optBuilder = new StringBuilder();
                foreach (var item in opt)
                {
                    optBuilder.Append(@"<div>" + item + "</div>");
                }

                str.Append("<tr>");
                str.Append(@"<td valign='top' style='padding:5px 15px; font-family: Arial,sans-serif; font-size: 16px; line-height:20px;border: 1px solid #ccc;margin-right:10px;'><div style='position: relative;left: 15px;'>" + user_question_selected_language.Question + "</div></td>");
                str.Append(@"<td valign='top' style='padding:5px 15px; font-family: Arial,sans-serif; font-size: 16px; line-height:20px;border: 1px solid #ccc;margin-right:10px;'><div style='position: relative;left: 15px;'>" + optBuilder.ToString() + "</div></td>");
                str.Append("<td valign='top' style='padding:5px 15px;; font-family: Arial,sans-serif; font-size: 16px; line-height:20px;border: 1px solid #ccc;margin-right:10px;'><div style='position: relative;left: 15px;'>" + qusAnsw.answer + "</div></td>");
                str.Append("</tr>");
            }
            str.Append("</tbody></table>");

            SendMail($"Summary Report of {meeting.Name}", str.ToString(), user.Email);
            return true;
        }

        public DataTable DownloadReport(long meetingId,int languageId)
        {
            var meeting = db.Meetings.AsNoTracking().FirstOrDefault(s => s.Id == meetingId);
            var meetingUsers = db.MeetingUsers.AsNoTracking().Where(s => s.MeetingId == meetingId).ToList();
            var meetingQuestions = db.MeetingQuestions.AsNoTracking().Where(s => s.MeetingId == meetingId).ToList();

            var meetingAnswers = db.MeetingAnswers.AsNoTracking().Where(s => s.MeetingId == meetingId).ToList();


            var dt = new DataTable();

            dt.Columns.Add("Participant");
            dt.Columns.Add("Email");
            dt.Columns.Add("Voteheld",typeof(long));

            foreach (var question in meetingQuestions.Where(s=>s.LanguageId==languageId)) 
            {
                dt.Columns.Add(question.Question);
            }

            foreach (var user in meetingUsers)
            {
                var row=dt.Rows.Add();
                row["Participant"] = user.FirstName + " " + user.LastName;
                row["Email"] = user.Email;
                row["Voteheld"] = user.Voteheld;

                foreach(var ques in meetingQuestions.Where(s => s.LanguageId == languageId))
                {
                    var user_question_Answer = meetingQuestions.FirstOrDefault(s =>s.MeetingId==meetingId && s.LanguageId == (int)Language.english && s.QuestionNumber==ques.QuestionNumber);
                    var user_question_selected_language = meetingQuestions.FirstOrDefault(s => s.MeetingId == meetingId && s.LanguageId == languageId && s.QuestionNumber == ques.QuestionNumber);
                    var available_options_english = meetingQuestions.FirstOrDefault(s => s.MeetingId == meetingId && s.LanguageId == (int)Language.english && s.QuestionNumber == ques.QuestionNumber).Options.Split(':');

                    var available_options_selectedLanguage = meetingQuestions.FirstOrDefault(s => s.MeetingId == meetingId && s.LanguageId == languageId && s.QuestionNumber == ques.QuestionNumber).Options.Split(':');

                    var userAns = meetingAnswers.Where(s => s.MeetingId == meetingId && s.QuestionId== user_question_Answer.QuestionNumber).ToList();
                    if (userAns.Count > 0)
                    {
                        var val = userAns.FirstOrDefault(s => s.QuestionId == user_question_Answer.QuestionNumber);
                        if (val != null)
                        {
                            if (available_options_english.Contains(val.UserAnswer))
                            {
                                var position = available_options_english.ToList().IndexOf(val.UserAnswer);
                                row[ques.Question] = available_options_selectedLanguage[position];
                            }
                        }
                    }

                }
            }

            return dt;
        }
      
        public List<QuestionAnswerResultDTO> GetMeetingResult(VoteResponseInputDTO inputDTO)
        {
            var questions = db.MeetingQuestions.Where(s => s.Meeting.Id == inputDTO.MeetingId).ToList();
            var userAns = db.MeetingAnswers.Where(s => s.MeetingId == inputDTO.MeetingId).ToList();
            List<QuestionAnswerResultDTO> qusAns = new List<QuestionAnswerResultDTO>();
            foreach (var question in questions)
            {
                QuestionAnswerResultDTO result = new QuestionAnswerResultDTO();
                result.meetingid = question.MeetingId;
                result.notes = question.Notes;
                result.qn_no = question.QuestionNumber;
                result.question = question.Question;
                result.type = question.Type;
                var res = new Dictionary<string, List<KeyValuePair<string, long>>>();
                List<KeyValuePair<string, long>> val = new List<KeyValuePair<string, long>>();
                var lstOptions = question.Options.Split(':');
                //var spanishOptions = question.Options_Spanish.Split(':');
                var langoption = new List<OptionsDto>();
                for (int i = 0; i < lstOptions.Length; i++)
                {
                    langoption.Add(new OptionsDto()
                    {
                        English = lstOptions[i],
                        //Spanish = spanishOptions[i] 
                        Swedish = ""

                    });

                }
                res.Add("english", new List<KeyValuePair<string, long>>());
                //res.Add("spanish", new List<KeyValuePair<string, long>>());
                foreach (var option in langoption)
                {
                    int count = userAns.Where(s => (s.UserAnswer.Trim() == option.English || s.UserAnswer.Trim() == option.Swedish) && s.QuestionId == question.Id).Count();
                    //val.Add(new KeyValuePair<string, long>(option, count));
                    var eng = res.FirstOrDefault(s => s.Key == "english");
                    var swedish = res.FirstOrDefault(s => s.Key == "swedish");
                    eng.Value.Add(new KeyValuePair<string, long>(option.English, count));
                    swedish.Value.Add(new KeyValuePair<string, long>(option.Swedish, count));
                }
                result.results = res;
                qusAns.Add(result);
            }
            return qusAns;
        }

        public QuestionAnswerResultDTO GetMeetingQuestionResult(QuestionInputDTO inputDTO)
        {
            var question = db.MeetingQuestions.FirstOrDefault(s => s.MeetingId == inputDTO.MeetingId && s.QuestionNumber == inputDTO.QuestionId);
            var lang_questiion = db.MeetingQuestions.FirstOrDefault(s => s.LanguageId == inputDTO.LanguageId && s.MeetingId == inputDTO.MeetingId && s.QuestionNumber == question.QuestionNumber);
            var userAns = db.MeetingAnswers.Where(s => s.MeetingId == inputDTO.MeetingId && s.QuestionId == inputDTO.QuestionId).ToList();

            QuestionAnswerResultDTO result = new QuestionAnswerResultDTO();
            result.meetingid = lang_questiion.MeetingId;
            result.notes = lang_questiion.Notes;
            result.qn_no = lang_questiion.QuestionNumber;
            result.question = lang_questiion.Question;
            result.type = lang_questiion.Type;
            List<KeyValuePair<string, long>> val = new List<KeyValuePair<string, long>>();
            var lstOptions = question.Options.Split(':');
            var grps = (from meeting in db.Meetings.AsNoTracking()

                        join meetingQuestion in db.MeetingQuestions.AsNoTracking() on meeting.Id equals meetingQuestion.MeetingId
                        join meetingUsers in db.MeetingUsers.AsNoTracking() on meeting.Id equals meetingUsers.MeetingId
                        join meetingAnswers in db.MeetingAnswers.AsNoTracking() on new { Id = meeting.Id, questionId = meetingQuestion.QuestionNumber, userId = meetingUsers.Id } equals new { Id = meetingAnswers.MeetingId, questionId = meetingAnswers.QuestionId, userId = meetingAnswers.UserId }
                        where meeting.Id == inputDTO.MeetingId && meetingAnswers.QuestionId == inputDTO.QuestionId
                        && meetingQuestion.LanguageId == ((int)Language.english)
                        select new
                        {
                            meetingId = meeting.Id,
                            questionId = meetingAnswers.QuestionId,
                            userId = meetingAnswers.UserId,
                            voteheld = meetingUsers.Voteheld,
                            userAnswer = meetingAnswers.UserAnswer
                        }).Distinct().ToList().GroupBy(s => s.userAnswer).ToList();

            var postalvotes = question.PostalVotes.Split(':');
            var arr = new Dictionary<string, long>();
            for (int i = 0; i < lstOptions.Length - 1; i++)
            {
                long voteWeight = 0;
                try
                {
                    voteWeight = Convert.ToInt64(postalvotes[i]);
                }
                catch (Exception ex)
                {
                    voteWeight = 0;
                }
                arr.Add(lstOptions[i], voteWeight);
            }

            var res = new List<KeyValuePair<string, long>>();

            foreach (var item in grps)
            {
                var voteWeight = item.Sum(s => s.voteheld);
                voteWeight = voteWeight + arr.FirstOrDefault(s => s.Key == item.Key).Value;
                val.Add(new KeyValuePair<string, long>(item.Key, voteWeight));
            }
            var keys = val.Select(s => s.Key).ToList();
            var missingKeys = lstOptions.Except(keys.Intersect(lstOptions));

            foreach (var item in lstOptions)
            {
                var IsAvailableitem = val.Any(s => s.Key == item);
                if (!IsAvailableitem)
                {
                    res.Add(new KeyValuePair<string, long>(item, 0));
                }
                else
                {
                    var value = val.FirstOrDefault(s => s.Key == item).Value;
                    res.Add(new KeyValuePair<string, long>(item, value));
                }
            }

            var total = res.Sum(s => s.Value);
            List<KeyValuePair<string, long>> Percentagevalue = new List<KeyValuePair<string, long>>();
            if (total == 0)
            {
                foreach (var item in res)
                {
                    var percenage = 0;

                    Percentagevalue.Add(new KeyValuePair<string, long>(item.Key, percenage));
                }

            }
            else
            {
                foreach (var item in res)
                {
                    var percenage = (item.Value * 100 / total);
                    Percentagevalue.Add(new KeyValuePair<string, long>(item.Key, percenage));
                }
            }
            var available_options_english = question.Options.Split(':');
            var available_options_selectedLanguage = lang_questiion.Options.Split(':');
            if (inputDTO.LanguageId == (int)(Language.english))
            {
                result.results = Percentagevalue;
                return result;
            }
            else
            {
                var Percentage = new List<KeyValuePair<string, long>>();
                foreach (var item in Percentagevalue)
                {
                    var index = available_options_english.ToList().IndexOf(item.Key);
                    Percentage.Add(new KeyValuePair<string, long>(available_options_selectedLanguage[index], item.Value));
                }

                result.results = Percentage;
                return result;
            }
        }

        //public List<MeetingExportDTO> GetExportMeetingQuestions(string meetingLink)
        //{
        //    var userInfo = db.MeetingUsers.FirstOrDefault(s => s.Link.Trim() == meetingLink.Trim());

        //    var meetings = db.MeetingQuestions.Where(s => s.MeetingId == userInfo.MeetingId && s.LanguageId == 1).ToList();
        //    List<MeetingExportDTO> lst = new List<MeetingExportDTO>();

        //    foreach (var meet in meetings)
        //    {
        //        MeetingExportDTO meetinDet = new MeetingExportDTO() { MeetingID = meet.MeetingId.ToString(), UserID = userInfo.Id.ToString(), QuestionID = meet.Id.ToString(), Questions = meet.Question, Options = meet.Options, Answers = "" };

        //        lst.Add(meetinDet);
        //    }

        //    return lst;
        //}
    }
}
