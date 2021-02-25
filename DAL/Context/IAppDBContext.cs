using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;

namespace DAL.Context
{
    public interface IAppDBContext
    {
        DbSet<Users> Users { get; set; }
        DbSet<Meeting> Meetings { get; set; }
        DbSet<MeetingUser> MeetingUsers { get; set; }
        DbSet<MeetingQuestion> MeetingQuestions { get; set; }
        DbSet<MeetingAnswers> MeetingAnswers { get; set; }
        int SaveChanges();
    }
}
