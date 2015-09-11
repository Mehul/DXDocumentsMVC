using DevExpress.Web;
using DevExpress.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.UI.WebControls;

namespace DXDocsMVC.Code
{
	 public static class UtilsMvc
	 {
		  static MVCxMenuItem CreateMenuItem(string text, string name, string imageUrl = "",
				bool beginGroup = false, bool clientVisible = true, bool selected = false,
				string cssClass = "", params MVCxMenuItem[] childItems)
		  {
				MVCxMenuItem result = new MVCxMenuItem(text, name)
				{
					 BeginGroup = beginGroup,
					 ClientVisible = clientVisible,
					 Selected = selected
				};
				if (!String.IsNullOrEmpty(imageUrl))
				{
					 result.Image.Url = imageUrl;
					 result.Image.Width = Unit.Pixel(16);
					 result.Image.Height = Unit.Pixel(16);
				}
				if (!String.IsNullOrEmpty(cssClass))
					 result.ItemStyle.CssClass = cssClass;

				if (childItems != null)
					 result.Items.AddRange(childItems);

				return result;
		  }

		  public static readonly MVCxMenuItem[] ActionMenuItems = new MVCxMenuItem[] 
		  {
				CreateMenuItem("New", "New", "/Content/Images/MenuIcons/New.png", false, true, false, "",
					 CreateMenuItem("Folder", "Folder", "/Content/Images/Predefined/Folder.png"),	 
					 CreateMenuItem("Worksheet Document", "WorksheetDocument", "/Content/Images/Predefined/Spreadsheet.png"),
					 CreateMenuItem("Rich Text Document", "RichTextDocument", "/Content/Images/Predefined/RTF.png")
				),
				CreateMenuItem("Upload", "Upload", "/Content/Images/MenuIcons/Upload.png", false, true),
				CreateMenuItem("Refresh", "Refresh", "/Content/Images/MenuIcons/Refresh.png", true, false),
				CreateMenuItem("Rename", "Rename", "/Content/Images/MenuIcons/Rename.png", true, false),
				CreateMenuItem("Move", "Move", "/Content/Images/MenuIcons/Move.png", false, false),
				CreateMenuItem("Copy", "Copy", "/Content/Images/MenuIcons/Copy.png", false, false),
				CreateMenuItem("Delete", "Delete", "/Content/Images/MenuIcons/Delete.png", false, false),
				CreateMenuItem("Download", "Download", "/Content/Images/MenuIcons/Download.png", false, false)
		  };

		  public static readonly MVCxMenuItem[] FilterMenuItems = new MVCxMenuItem[]
		  {
				CreateMenuItem("All", "All", "", false, true, true, "FilterItem"),
				CreateMenuItem("My", "My", ""),
				CreateMenuItem("Recent", "Recent", ""),
				CreateMenuItem("RTF Docs", "RTFDocs", ""),
				CreateMenuItem("Sheets", "Sheets", "")
		  };

		  public static MVCxMenuItem[] GetUserMenuItems(string username, string url)
		  {
				var result = CreateMenuItem(username, "User", url, false, true, false, "",
						  CreateMenuItem("Sign Out", "SignOut", "")
					 );
				result.Image.Width = Unit.Pixel(60);
				result.Image.Height = Unit.Pixel(60);

				return new MVCxMenuItem[] { result };
		  }

	 }
}