using DevExpress.Web.Mvc;
using DXDocsMVC.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXDocsMVC.Models
{
    public class Document
    {
        public DocumentsApp DocumentsApp { get; set; }

        public MVCxMenuItem[] UserMenuItems { get { return UtilsMvc.GetUserMenuItems(DocumentsApp.User.CurrentUser.Name, DocumentsApp.GetCurrentUserAvatarVirtPath()); } }

        public string TitleText { get; set; }
        public string DocumentName { get; set; }
        public string RibbonControlPartial { get; set; }
        public string EditorControlPartial { get; set; }

    }
}
