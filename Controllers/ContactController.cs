using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Mail;
using System.Diagnostics;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

using PhoneBook_CloudComputing.Models;

namespace PhoneBook_CloudComputing.Controllers
{
    public class ContactController : Controller
    {
        private PhoneBookEntities db = new PhoneBookEntities();
        public ActionResult Contacts()
        {
            if (Session["Id"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            int companyId = Int32.Parse(Session["CompanyId"].ToString());
            List<Contact> contacts = (from c in db.Contact
                                      where c.CompanyId == companyId
                                      select c).ToList<Contact>();
            ViewBag.Message = TempData["Message"];
            return View(contacts);
        }

        public ActionResult Add()
        {
            if (Session["Id"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public ActionResult AddSubmit()
        {
            string name = Request.Form["name"];
            string companyName = Request.Form["companyName"];
            string email = Request.Form["email"];
            string phoneNumber = Request.Form["phoneNumber"];
            string address = Request.Form["address"];

            Contact contact = new Contact();
            contact.Name = name;
            contact.CompanyName = companyName;
            contact.CompanyId = Int32.Parse(Session["CompanyId"].ToString());
            contact.Email = email;
            contact.PhoneNumber = phoneNumber;
            contact.Address = address;
            db.Contact.Add(contact);
            db.SaveChanges();

            return RedirectToAction("Details", new { id = contact.Id });
        }
        
        public ActionResult Details(string id)
        {
            if (Session["Id"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            int _id = Int32.Parse(id);
            Contact contact = (from c in db.Contact
                               where c.Id == _id
                               select c).FirstOrDefault<Contact>();
            ViewData["Contact"] = contact;
            return View();
        }

        public ActionResult Edit(string id)
        {
            if (Session["Id"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            int _id = Int32.Parse(id);
            Contact contact = (from c in db.Contact
                               where c.Id == _id
                               select c).FirstOrDefault<Contact>();
            ViewData["Contact"] = contact;
            return View();
        }

        [HttpPost]
        public ActionResult EditSubmit(string id)
        {
            int _id = Int32.Parse(Request.Form["id"].ToString());
            Contact contact = (from c in db.Contact
                               where c.Id == _id
                               select c).FirstOrDefault<Contact>();
            if (contact == null || !contact.CompanyId.Equals(Int32.Parse(Session["CompanyId"].ToString())))
            {
                TempData["Message"] = "Contact not found or you do not have permission to make changes!";
            }

            string name = Request.Form["name"];
            string companyName = Request.Form["companyName"];
            string email = Request.Form["email"];
            string phoneNumber = Request.Form["phoneNumber"];
            string address = Request.Form["address"];

            contact.Name = name;
            contact.CompanyName = companyName;
            contact.Email = email;
            contact.PhoneNumber = phoneNumber;
            contact.Address = address;
            db.SaveChanges();

            return RedirectToAction("Details", new { id = id });
        }

        public ActionResult Delete(string id)
        {
            if (Session["Id"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            int _id = Int32.Parse(id);
            Contact contact = (from c in db.Contact
                               where c.Id == _id
                               select c).FirstOrDefault<Contact>();
            if (contact == null || !contact.CompanyId.Equals(Int32.Parse(Session["CompanyId"].ToString())))
            {
                TempData["Message"] = "Contact not found or you do not have permission to make changes!";
            }

            db.Contact.Remove(contact);
            db.SaveChanges();

            return RedirectToAction("Contacts");
        }

        [HttpPost]
        public ActionResult SendMessage(string action)
        {
            var contactsStr = Request.Form["contact"];
            if(contactsStr == null)
            {
                TempData["Message"] = "Please choose at least 1 contact";
                return RedirectToAction("Contacts");
            }

            contactsStr = contactsStr.ToString();
            string nextAction = "";
            switch (action.Trim())
            {
                case "Send email to selected contact(s)":
                    nextAction = "Email";
                    break;
                case "Send SMS to selected contact(s)":
                    nextAction = "SMS";
                    break;
            }
            return RedirectToAction(nextAction, new { selected = contactsStr });
        }

        public ActionResult Email(string selected)
        {
            int companyId = Int32.Parse(Session["CompanyId"].ToString());
            List<string> list = selected.Split(',').ToList();
            List<Contact> selectedContacts = new List<Contact>();
            List<Contact> contacts = (from c in db.Contact
                                      where c.CompanyId == companyId
                                      select c).ToList<Contact>();
            foreach (string contact in list)
            {
                foreach (Contact c in contacts)
                {
                    if (c.Id.Equals(Int32.Parse(contact.Trim())))
                    {
                        selectedContacts.Add(c);
                        break;
                    }
                }
            }
            ViewData["ContactsStr"] = selected;
            ViewData["Contact"] = selectedContacts;
            return View();
        }

        [HttpPost]
        public ActionResult EmailSubmit()
        {
            string selected = Request.Form["contacts"];
            int companyId = Int32.Parse(Session["CompanyId"].ToString());
            List<string> list = selected.Split(',').ToList();
            List<Contact> selectedContacts = new List<Contact>();
            List<Contact> contacts = (from c in db.Contact
                                      where c.CompanyId == companyId
                                      select c).ToList<Contact>();
            foreach (string contact in list)
            {
                foreach (Contact c in contacts)
                {
                    if (c.Id == Int32.Parse(contact))
                    {
                        selectedContacts.Add(c);
                        break;
                    }
                }
            }

            int id = Int32.Parse(Session["Id"].ToString());
            Account account = (from a in db.Account
                               where a.Id == id
                               select a).FirstOrDefault<Account>();

            string subject = Request.Form["subject"].ToString();
            string body = Request.Form["body"].ToString();
            string password = Request.Form["password"].ToString();

            MailMessage mail = new MailMessage();
            foreach (Contact c in selectedContacts)
            {
                mail.To.Add(c.Email);
            }
            mail.From = new MailAddress(account.Email);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = false;

            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential(account.Email, password);
            smtp.EnableSsl = true;

            try 
            {
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                RedirectToAction("Details");
            }

            ViewData["Contact"] = selectedContacts;
            ViewData["Subject"] = subject;
            ViewData["Body"] = body;
            return View("EmailReview");
        }

        public ActionResult EmailReview()
        {
            if(ViewData["Body"] == null)
            {
                return RedirectToAction("Details");
            }
            return View();
        }

        public ActionResult SMS(string selected)
        {
            int companyId = Int32.Parse(Session["CompanyId"].ToString());
            List<string> list = selected.Split(',').ToList();
            List<Contact> selectedContacts = new List<Contact>();
            List<Contact> contacts = (from c in db.Contact
                                      where c.CompanyId == companyId
                                      select c).ToList<Contact>();
            foreach (string contact in list)
            {
                foreach (Contact c in contacts)
                {
                    if (c.Id == Int32.Parse(contact))
                    {
                        selectedContacts.Add(c);
                        break;
                    }
                }
            }
            ViewData["ContactsStr"] = selected;
            ViewData["Contact"] = selectedContacts;
            return View();
        }

        [HttpPost]
        public ActionResult SMSSubmit()
        {
            string selected = Request.Form["contacts"];
            int companyId = Int32.Parse(Session["CompanyId"].ToString());
            List<string> list = selected.Split(',').ToList();
            List<Contact> selectedContacts = new List<Contact>();
            List<Contact> contacts = (from c in db.Contact
                                      where c.CompanyId == companyId
                                      select c).ToList<Contact>();
            foreach (string contact in list)
            {
                foreach (Contact c in contacts)
                {
                    if (c.Id == Int32.Parse(contact))
                    {
                        selectedContacts.Add(c);
                        break;
                    }
                }
            }

            string content = Request.Form["content"].ToString();
            string accountSid = "AC289544aa20759b8c919e5f45d1fc6712";
            string authToken = "277a9b2b1f90e692f490561937a206f6";
            TwilioClient.Init(accountSid, authToken);

            for (int i = 0; i < selectedContacts.Count; i++)
            {
                string phone = "+84" + selectedContacts[i].PhoneNumber.Substring(1);
                var message = MessageResource.Create(
                    body: content,
                    from: new Twilio.Types.PhoneNumber("+17622525479"),
                    to: new Twilio.Types.PhoneNumber(phone)
                );
            }

            ViewData["Contact"] = selectedContacts;
            ViewData["Body"] = content;
            return View("SMSReview");
        }

        public ActionResult SMSReview()
        {
            if (ViewData["Body"] == null)
            {
                return RedirectToAction("Details");
            }
            return View();
        }
    }
}