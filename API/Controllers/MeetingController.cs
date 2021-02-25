using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DAL.Context;
using DAL.Entities;
using System.Net.Mail;
using System.Net;
using BAL;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using API.Hubs;
using BAL.Helpers;
using System.Data;
using Newtonsoft.Json;
using OfficeOpenXml;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MeetingController : Controller
    {
        IMeetingServices meetingServices;
        private IConfiguration config;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IHubContext<ChatHub> _hubcontext;
        public MeetingController(IMeetingServices _meetingServices, IConfiguration _config, IHostingEnvironment hostingEnvironment, IHubContext<ChatHub> hubcontext)
        {
            meetingServices = _meetingServices;
            config = _config;
            _hostingEnvironment = hostingEnvironment;
            _hubcontext = hubcontext;
        }

        [Route("addmeeting")]
        [HttpPost]
        public IActionResult AddMeeting([FromForm] MeetingInputDTO meetingDetails)
        {
            string baseURL = config["meetingbaseURL"];
            var MeetingOwnerUrl = Path.Combine(_hostingEnvironment.ContentRootPath, config["OTPMailtemplateURL"]);
            var MeetingOwnerContent = System.IO.File.ReadAllText(MeetingOwnerUrl);
            var meetingDetail = meetingServices.AddMeeting(meetingDetails, baseURL, MeetingOwnerContent);
            return Ok(meetingDetail);
        }
        [HttpGet]
        [Route("SendMeetingInvitation")]
        public IActionResult SendMeetingRequestEmail(long meetingId)
        {
            string baseURL = config["meetingbaseURL"];
            var InvitationUrl = Path.Combine(_hostingEnvironment.ContentRootPath, config["mailtemplateURL"]);
            var InvitationContent = System.IO.File.ReadAllText(InvitationUrl);
            var isMeetingLinkSended = meetingServices.SendMeetingRequestEmail(meetingId, baseURL, InvitationContent);
            return Ok(isMeetingLinkSended);
        }

        [Route("updatemeeting")]
        [HttpPost]
        public IActionResult UpdateMeeting([FromForm] MeetingInputDTO meetingDetails)
        {
            var meetingDetail = meetingServices.UpdateMeeting(meetingDetails);
            return Ok(meetingDetail);
        }

        [HttpGet]
        [Route("allmeetinginfo")]
        public IActionResult GetAllMeeting(long LoginUserId)
        {
            var meetingDetail = meetingServices.GetAllMeeting(LoginUserId);
            return Ok(meetingDetail);
        }

        [HttpGet]
        [Route("meetinginfo")]
        public IActionResult GetMeeting(int meetingID)
        {
            var meetingDetail = meetingServices.GetMeeting(meetingID);
            return Ok(meetingDetail);
        }

        [HttpGet]
        [Route("meetingusersinfo")]
        public IActionResult GetAllMeetingUsers(int meetingID)
        {
            var meetingDetail = meetingServices.GetAllMeetingUsers(meetingID);
            return Ok(meetingDetail);
        }

        [HttpGet]
        [Route("meetingquestionsinfo")]
        public IActionResult GetAllMeetingQuestions(int meetingID)
        {
            var meetingDetail = meetingServices.GetAllMeetingQuestions(meetingID);
            return Ok(meetingDetail);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("join")]
        public IActionResult JoinMeeting([FromQuery] string meetingLink)
        {

            var url = Path.Combine(_hostingEnvironment.ContentRootPath, config["OTPMailtemplateURL"]);
            var mailbody = System.IO.File.ReadAllText(url);
            return Ok(meetingServices.ValidateMeetingLink(meetingLink, mailbody));
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("validateotp")]
        public IActionResult ValidateOTP([FromForm] ValidateMeetingInputDTO dto)
        {
            var userInfo = meetingServices.ValidateOTP(dto);
            if (userInfo != null)
            {
                var singalRSession = new UserSession() { MeetingId = dto.MeetingId, MeetingLink = userInfo.Link, SessionBrowserRandomId = Guid.NewGuid().ToString(), UserId = userInfo.Id, FirstName = userInfo.FirstName, LastName = userInfo.LastName };
                var activeUserSession = (ChatHub.userSessions.FirstOrDefault(s => s.MeetingId == dto.MeetingId && s.UserId == userInfo.Id));
                if (activeUserSession != null)
                {
                    if (activeUserSession.ConnectionId == null)
                    {
                        singalRSession.SessionBrowserRandomId = activeUserSession.SessionBrowserRandomId;
                        ChatHub.userSessions.Remove(activeUserSession);
                    }
                    else
                    {
                        _hubcontext.Clients.Client(activeUserSession.ConnectionId).SendAsync("usersessionresponse", "Deny");
                        ChatHub.userSessions.Remove(activeUserSession);
                    }
                }
                ChatHub.userSessions.Add(singalRSession);
                return Ok(singalRSession);
            }
            var isValidSession = userInfo != null;
            return Ok(isValidSession);

        }

        [HttpPost]
        [AllowAnonymous]
        [Route("StoreUserAnswer")]
        public IActionResult StoreUserAnswer([FromBody] UserAnswerInputDTO dto)
        {
            return Ok(meetingServices.StoreUserPollAnswer(dto));
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("summaryresult")]
        public IActionResult GetUserQuestionAnswer([FromBody] VoteResponseInputDTO dto)
        {
            return Ok(meetingServices.GetUserQuestionAnswer(dto));
        }


        [HttpGet]
        [AllowAnonymous]
        [Route("send/summaryreport/mail")]
        public IActionResult SendSummaryReort(long userId)
        {
            return Ok(meetingServices.SendSummaryReort(userId));
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("download/report")]
        public ActionResult DownloadReport(long meetingId, int languageId)
        {
            //int meetingId = 26;
            //int languageId = 1;
            var dt = meetingServices.DownloadReport(meetingId,languageId);
            var meeting_deails = meetingServices.GetMeeting(meetingId);
            var package = new ExcelPackage();
            var columns_count = dt.Columns.Count;
            // add a new worksheet to the empty workbook
            var ws = package.Workbook.Worksheets.Add("Sheet1");
            ws.Cells[1, 1, 1, columns_count].Merge = true;
            ws.Cells[1,1,1,columns_count].Value = "Voting Report";
            ws.Cells[1, 1, 1, columns_count].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            ws.Cells[1, 1, 1, columns_count].Style.Font.Bold = true;

            ws.Cells["A3"].Value = "Meeting Id";
            ws.Cells["A3"].Style.Font.Bold = true;
            ws.Cells["B3"].Value = meeting_deails.m_id.ToString();

            ws.Cells["D3"].Value = "MeetinName";
            ws.Cells["D3"].Style.Font.Bold = true;
            ws.Cells["E3"].Value = meeting_deails.m_name;

            ws.Cells["A4"].Value = "Meeting Owner";
            ws.Cells["A4"].Style.Font.Bold = true;
            ws.Cells["B4"].Value = meeting_deails.m_owner;

            ws.Cells["D4"].Value = "Date&Time";
            ws.Cells["D4"].Style.Font.Bold = true;
            ws.Cells["E4"].Value = meeting_deails.m_date.ToString("dd-MMM-yyyy hh:mm tt");

            ws.Cells["A6:AZ6"].Style.Font.Bold = true;
            ws.Cells["A6"].LoadFromDataTable(dt, true);
            ws.Cells.AutoFitColumns();
            var fileGuid = Guid.NewGuid().ToString();
            var filePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Files", $"{ fileGuid}.xlsx");
            var file = System.IO.File.Create(filePath);
            package.SaveAs(file);
            file.Close();
            byte[] fileByteArray = System.IO.File.ReadAllBytes(filePath);
            System.IO.File.Delete(filePath);
            return File(fileByteArray, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{meeting_deails.m_name}_{meetingId}_{DateTime.Now.ToString("ddMMMyyyyHHmmss")}.xlsx");
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("file/download")]
        public ActionResult Download(string fileName)
        {
            var filePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Files", fileName);
            byte[] fileByteArray = System.IO.File.ReadAllBytes(filePath);
            System.IO.File.Delete(filePath);
            return File(fileByteArray, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("QuestionResponseResult")]
        public IActionResult QuestionResponseResult([FromBody] QuestionInputDTO dto)
        {
            return Ok(meetingServices.GetMeetingQuestionResult(dto));
        }


    }
}
