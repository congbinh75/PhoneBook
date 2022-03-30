using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using PhoneBook_CloudComputing.Models;

namespace PhoneBook_CloudComputing.Controllers
{
    public class AccountController : Controller
    {
        private PhoneBookEntities db = new PhoneBookEntities();

        public string HashPassword(string password)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            Byte[] originalBytes = ASCIIEncoding.Default.GetBytes(password);
            Byte[] encodedBytes = md5.ComputeHash(originalBytes);

            return BitConverter.ToString(encodedBytes);
        }

        public ActionResult Accounts()
        {
            int companyId = Int32.Parse(Session["CompanyId"].ToString());
            if (Session["Id"] == null || Int32.Parse(Session["Role"].ToString()) != 0 )
            {
                return RedirectToAction("Index", "Home");
            }
            List<Account> accounts = (from a in db.Account
                                      where a.CompanyId == companyId
                                      select a).ToList<Account>();
            return View(accounts);
        }

        public ActionResult LogIn()
        {
            if(Session["Id"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        public ActionResult Register()
        {
            if (Session["Id"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        public ActionResult LogOut()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogInSubmit(Account account)
        {
            if(ModelState.IsValid)
            {
                string hashedPassword = HashPassword(account.Password);
                Debug.WriteLine(hashedPassword);
                Account result = (from a in db.Account
                                  where (a.Email.Equals(account.Email) && a.Password.Equals(hashedPassword))
                                  select a).FirstOrDefault<Account>();
                if (result != null)
                {
                    Session["Id"] = result.Id;
                    Session["CompanyId"] = result.CompanyId;
                    Session["Role"] = result.Role;
                    return RedirectToAction("Index", "Home");
                }
                ViewBag.Message = "Username or password is incorrect!";
                return RedirectToAction("LogIn");
            }
            ViewBag.Message = "Something is wrong! Please try again!";
            return RedirectToAction("LogIn");
        }

        [HttpPost]
        public ActionResult RegisterSubmit()
        {
            string companyName = Request.Form["inputCompanyName"].ToString();
            string name = Request.Form["inputName"];
            string email = Request.Form["inputEmail"].ToString();
            string password = Request.Form["inputPassword"].ToString();
            string confirmPassword = Request.Form["inputConfirmPassword"].ToString();

            Account acc = (from a in db.Account
                           where a.Email.Equals(email)
                           select a).FirstOrDefault<Account>();
            if (acc != null)
            {
                ViewBag.Message ="Email is already registered!";
                return View("Register");
            }

            if(!password.Equals(confirmPassword))
            {
                ViewBag.Message = "Password and confirmation do not match!";
                return View("Register");
            }

            Company company = new Company();
            company.Name = companyName;
            db.Company.Add(company);
            db.SaveChanges();

            Account account = new Account();
            account.Name = name;
            account.CompanyId = company.Id;
            account.Email = email;
            account.Password = HashPassword(password);
            account.Role = 0;
            db.Account.Add(account);
            db.SaveChanges();

            return RedirectToAction("LogIn");
        }

        public ActionResult Add()
        {
            if (Session["Id"] == null || Int32.Parse(Session["Role"].ToString()) != 0)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult AddSubmit()
        {
            string name = Request.Form["name"].ToString();
            string email = Request.Form["email"].ToString();
            string password = Request.Form["password"].ToString();
            string confirmPassword = Request.Form["confirmPassword"].ToString();

            if (!password.Equals(confirmPassword))
            {
                ViewBag.Message("Password and confirmation do not match!");
                return Redirect("Add");
            }

            Account account = new Account();
            account.Name = name;
            account.Email = email;
            account.Password = HashPassword(password);
            account.Role = 1;
            account.CompanyId = Int32.Parse(Session["CompanyId"].ToString());
            db.Account.Add(account);
            db.SaveChanges();

            return RedirectToAction("Accounts");
        }

        public ActionResult Delete(string id)
        {
            if (Session["Id"] != null || Int32.Parse(Session["Role"].ToString()) != 0)
            {
                return RedirectToAction("Index", "Home");
            }

            int _id = Int32.Parse(id);
            Account account = (from a in db.Account
                               where a.Id == _id
                               select a).FirstOrDefault<Account>();
            if (account == null || !account.CompanyId.Equals(Session["CompanyId"].ToString()))
            {
                TempData["Message"] = "Contact not found or you do not have permission to make changes!";
            }

            db.Account.Remove(account);
            db.SaveChanges();

            return RedirectToAction("Accounts");
        }

        public ActionResult Details(string id)
        {
            if (Session["Id"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            int _id = Int32.Parse(id);
            Account account = (from a in db.Account
                               where a.Id == _id
                               select a).FirstOrDefault<Account>();
            ViewData["Account"] = account;

            Company company = (from c in db.Company
                               where c.Id == account.CompanyId
                               select c).FirstOrDefault<Company>();
            ViewData["Company"] = company;

            return View();
        }

        public ActionResult Edit(string id)
        {
            if (Session["Id"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            int _id = Int32.Parse(id);
            Account account = (from a in db.Account
                              where a.Id == _id
                              select a).FirstOrDefault<Account>();
            ViewData["Account"] = account;

            Company company = (from c in db.Company
                               where c.Id == account.CompanyId
                               select c).FirstOrDefault<Company>();
            ViewData["Company"] = company;

            return View();
        }

        [HttpPost]
        public ActionResult EditSubmit()
        {
            string name = Request.Form["name"].ToString();

            int id = Int32.Parse(Session["Id"].ToString());
            Account account = (from a in db.Account
                               where a.Id == id
                               select a).FirstOrDefault<Account>();
            account.Name = name;
            db.SaveChanges();

            if (Int32.Parse(Session["Role"].ToString()) == 0)
            {
                string companyName = Request.Form["companyName"].ToString();
                int companyId = Int32.Parse(Session["CompanyId"].ToString());
                Company company = (from c in db.Company
                                   where c.Id == companyId
                                   select c).FirstOrDefault<Company>();
                company.Name = name;
                db.SaveChanges();
            }

            return RedirectToAction("Details");
        }

        public ActionResult ChangePassword()
        {
            if (Session["Id"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Message = TempData["Message"];
            return View();
        }

        [HttpPost]
        public ActionResult ChangePasswordSubmit()
        {
            string newPassword = Request.Form["newPassword"].ToString();
            string repeatNewPassword = Request.Form["repeatNewPassword"].ToString();

            if (!newPassword.Equals(repeatNewPassword))
            {
                TempData["Message"] = "New password and confirmation do not match!";
                return RedirectToAction("ChangePassword");
            }

            int id = Int32.Parse(Session["Id"].ToString());
            Account result = (from a in db.Account
                              where a.Id == id
                              select a).FirstOrDefault<Account>();
            result.Password = HashPassword(newPassword);
            db.SaveChanges();

            return RedirectToAction("Details");
        }
    }
}