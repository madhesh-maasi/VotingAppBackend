using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BAL.Helpers;
using CsvHelper;
using DAL;
using DAL.Context;
using DAL.Entities;
using DTO;
using Microsoft.EntityFrameworkCore;

namespace BAL
{
    public interface IUserServices
    {
        public LoginInputDTO GetUserInfo(string userName, string password);
    }

    public class UserServices : IUserServices
    {
        IAppDBContext db;
        public UserServices(IAppDBContext db1)
        {
            db = db1;
        }

        public LoginInputDTO GetUserInfo(string userName, string password)
        {
            Users user = default(Users);
            if (userName.Contains("@"))
            {
                user = db.Users.FirstOrDefault(s => s.Email == userName && s.Password == password);
            }
            else
            {
                user = db.Users.FirstOrDefault(s => s.Username == userName && s.Password == password);
            }
            LoginInputDTO userDet = null;
            if (user != null)
            {
                userDet = new LoginInputDTO() { Email = user.Email, Id = user.Id, MobileNo = user.MobileNo, Password = user.Password, UserName = user.Username, Role=user.Role };
            }
            return userDet;
        }
    }
}
