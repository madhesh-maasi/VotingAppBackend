using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using DAL.Entities;

namespace DAL.Context
{
    public class AppDBContext : DbContext, IAppDBContext
    {

        public DbSet<Users> Users { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<MeetingUser> MeetingUsers { get; set; }
        public DbSet<MeetingQuestion> MeetingQuestions { get; set; }

        public DbSet<MeetingAnswers> MeetingAnswers { get; set; }

        public DbSet<Languages> Languages { get; set; }

        public AppDBContext(DbContextOptions<AppDBContext> options)
           : base(options)
        {

        }
    }
}

