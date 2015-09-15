using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DXDocsMVC.Controllers
{
    public class DocumentController : Controller
    {
        // GET: Document
        public ActionResult All(string filePath)
        {
            return View();
        }


        public ActionResult CallbackPanelPartial()
        {
            return PartialView("_CallbackPanelPartial");
        }
    }
}