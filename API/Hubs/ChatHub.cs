using BAL;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using DTO;

namespace API.Hubs
{
    public class ChatHub : Hub
    {
        public static List<UserSession> userSessions = new List<UserSession>();

        public static List<MeetingSession> meetingSessions = new List<MeetingSession>();

        private readonly IMeetingServices _meetingServices;

        public ChatHub(IMeetingServices meetingServices)
        {
            _meetingServices = meetingServices;
        }
        public async Task SendConnectionId(string connectionId)
        {
            await Clients.All.SendAsync("setClientMessage", "A connection with ID '" + connectionId + "' has just connected");
        }
        [HubMethodName("releasequestion")]
        public async Task releasequestion(releaseQuestionInputDto dto)
        {
            var meeting = meetingSessions.FirstOrDefault(s => s.MeetingId == dto.meetingId);
            if (meeting != null)
            {
                meeting.ReleaseQuestions = meeting.ReleaseQuestions + 1;
            }
            else
            {
                var meetingSession = new MeetingSession() { MeetingId = dto.meetingId, ReleaseQuestions = 1 };
                meetingSessions.Add(meetingSession); 
            }
            await Clients.OthersInGroup(dto.meetingId.ToString()).SendAsync("changequestion", dto);
        }

        [HubMethodName("usersession")]
        public async Task usersession(UserSessionIputDto userModel)
        {
            var currentUser = userSessions.FirstOrDefault(s => s.MeetingLink == userModel.MeetingLink);
            if (currentUser != null)
            {
                if (currentUser.ConnectionId == this.Context.ConnectionId)
                {
                    await Clients.Caller.SendAsync("usersessionresponse", "Allow");
                }
                else if (currentUser.SessionBrowserRandomId == userModel.SessionBrowserRandomId)
                {
                    var currentUsersession = userSessions.FirstOrDefault(s => s.SessionBrowserRandomId == userModel.SessionBrowserRandomId && s.UserId == userModel.UserId);
                  

                    if (!currentUsersession.UserCheckin)
                    {
                        var meetingRoom = meetingSessions.FirstOrDefault(s => s.MeetingId == userModel.MeetingId);
                        if (meetingRoom != null)
                        {
                            if (meetingRoom.ReleaseQuestions > 0)
                            {
                                await Clients.Caller.SendAsync("usersessionresponse", "MeetingAlreadyStarted");
                                return;
                            }
                        }
                    }

                    userSessions.Remove(currentUsersession);
                    await Groups.AddToGroupAsync(Context.ConnectionId, userModel.MeetingId.ToString());
                    var connectionId = this.Context.ConnectionId;

                    var session = new UserSession()
                    {
                        ConnectionId = connectionId,
                        MeetingId = userModel.MeetingId,
                        UserId = userModel.UserId,
                        MeetingLink = userModel.MeetingLink,
                        SessionBrowserRandomId = userModel.SessionBrowserRandomId,
                        UserCheckin=true
                    };
                    userSessions.Add(session);
                    await Clients.Caller.SendAsync("usersessionresponse", "Allow");
                }
                else
                {
                    await Clients.Caller.SendAsync("usersessionresponse", "Deny");
                }
            }
            else
            {
                var meetingRoom = meetingSessions.FirstOrDefault(s => s.MeetingId == userModel.MeetingId);
                if (meetingRoom != null)
                {
                    if (meetingRoom.ReleaseQuestions > 1)
                    {
                        await Clients.Caller.SendAsync("usersessionresponse", "MeetingAlreadyStarted");
                    }
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, userModel.MeetingId.ToString());
                var connectionId = this.Context.ConnectionId;
                var session = new UserSession()
                {
                    ConnectionId = connectionId,
                    MeetingId = userModel.MeetingId,
                    UserId = userModel.UserId,
                    MeetingLink = userModel.MeetingLink,
                    SessionBrowserRandomId = userModel.SessionBrowserRandomId
                };
                userSessions.Add(session);
                await Clients.Caller.SendAsync("usersessionresponse", "Allow");
            }
        }

        [HubMethodName("showresult")]
        public async Task ShowResult(showresultInputDto dto)
        {
            await Clients.OthersInGroup(dto.meetingId.ToString()).SendAsync("showresponse", dto);
        }

        [HubMethodName("completepoll")]
        public async Task CompletePoll(completepollInputdto dto)
        {
            var response = _meetingServices.CompletePoll(new CompletePollDto() { meetingId = dto.meetingId });
            var meetingSession=meetingSessions.FirstOrDefault(s => s.MeetingId == dto.meetingId);
            meetingSessions.Remove(meetingSession);
            await Clients.OthersInGroup(dto.meetingId.ToString()).SendAsync("pollcomplete", response);
        }
        [HubMethodName("Disconnect")]
        public void Disconnect()
        {
            var connectionId = this.Context.ConnectionId;
            userSessions.Remove(userSessions.FirstOrDefault(s => s.ConnectionId == connectionId));
        }
    }
}
