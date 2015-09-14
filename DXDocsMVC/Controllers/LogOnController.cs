using DXDocsMVC.Code;
using DXDocsMVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DXDocsMVC.Controllers
{
    public class LogOnController : Controller
    {
        // GET: LogOn
        public ActionResult Index()
        {
            var model = new LogOnModel
            {
                AccountName = UserService.DefaultUserAccountName,
                UserPassword = null,
                ErrorText = String.Empty
            };
            return View(model);
        }

        //
        [HttpPost]
        public ActionResult Index([ModelBinder(typeof(DevExpress.Web.Mvc.DevExpressEditorsBinder))]LogOnModel postedModel)
        {
            UserService userService = DocumentsApp.Instance.User;
            LogOnModel model;
            if (!userService.SignIn(postedModel.AccountName, null))
            {
                model = new LogOnModel
                {
                    AccountName = postedModel.AccountName,
                    UserPassword = null,
                    ErrorText = String.Format("Login failed for '{0}'. Make sure your account name is correct and retype the password in the correct case.", postedModel.AccountName)
                };
            }
            else
            {
                model = new LogOnModel
                {
                    AccountName = postedModel.AccountName,
                    UserPassword = null,
                    ErrorText = String.Empty
                };
            }
            return View(model);
        }
    }
}