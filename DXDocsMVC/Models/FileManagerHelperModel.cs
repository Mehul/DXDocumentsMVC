using DevExpress.Web;
using DXDocsMVC.Code;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DXDocsMVC.Models
{
	 public class FileManagerHelperModel
	 {
		  
		  public string ThumbImageStyle { get; set; }
		  public string IconImageSrc { get; set; }
		  public string IconImageAlt { get; set; }
		  public string IconImageDetailsSrc { get; set; }
		  public string ImageHolderHRef { get; set; }
		  public string ImageHolderOnDown { get; set; }
		  public string ImageHolderOnClick { get; set; }
		  public string ImageHolderTarget { get; set; }

		  public string ItemLockStyle { get; set; }
		  public string ItemLockSrc { get; set; }
		  public string ItemLockAlt { get; set; }
		  public string ItemLockTitle { get; set; }
		  public string FooterClass { get; set; }

		  public string IconImageStyle { get; set; }
		  public string ModifiedText { get; set; }
		  public string Name { get; set; }
		  public string OwnerText { get; set; }
		  public string SizeText { get; set; }

		  static string GetModifiedInfoString(DateTime date)
		  {
				string format = date.Date == DateTime.Today ? "H:mm" : "d MMM yyy";
				return date.ToString(format);
		  }

		  static string ToAbsoluteUrl(string url)
		  {
				if (String.IsNullOrEmpty(url))
					 return String.Empty;
				
				string result = url.Replace("\\", "/").Replace("~/", "/");
				if (result.StartsWith("/"))
					 return result;
				return String.Format("/{0}", result);
		  }

		  public FileManagerHelperModel(FileManagerItem fileManagerItem, DocumentsApp app, string sourceName)
		  {
				long itemId = long.Parse(fileManagerItem.Id);
				Item item = app.Data.FindItemById(itemId);

				Name = fileManagerItem.Name;
				ModifiedText = String.Format("Modified {0}", GetModifiedInfoString(fileManagerItem.LastWriteTime));
				OwnerText = (item.Owner.Id == app.User.CurrentUser.Id) ? "Me" : item.Owner.Name;
				SizeText = item.IsFolder ? "" : CommonUtils.GetSizeString(fileManagerItem.Length);
			
				ThumbImageStyle = String.Format("background-image:url({0})", ToAbsoluteUrl(fileManagerItem.ThumbnailUrl));

				IconImageSrc = ToAbsoluteUrl(app.Image.GetFooterItemIconVirtPath(item));									 
				IconImageAlt = "Item type here";
				IconImageStyle = String.IsNullOrEmpty(IconImageSrc) ? "display: none;" : "";
				IconImageDetailsSrc = ToAbsoluteUrl(app.Image.GetDetailsViewItemIcon(item) ?? fileManagerItem.ThumbnailUrl);

				ImageHolderHRef = "javascript:;";
				ImageHolderOnDown = "App.onItemLinkMouseDown(event)";

				if (item.IsFolder)
					 ImageHolderOnClick = "App.onFolderItemLinkClick(event)";
				else
				{
					 ImageHolderOnClick = "App.onFileItemLinkClick(event)";
					 if (app.Document.IsDocumentEditingAllowed(item))
						  ImageHolderTarget = "_blank";

					 string href = app.Document.GetDocumentEditorRequestUrl(fileManagerItem.FullName, sourceName);
					 ImageHolderHRef = href; //.HtmlEncode(href);
				}

				FooterClass = "itemFooter";
				User user = app.Document.GetDocumentLockOwner(item);
				if (user != null)
				{
					 ItemLockStyle = "display: block;";					 
					 if (user.Id == app.User.CurrentUser.Id)
					 {
						  ItemLockSrc = ToAbsoluteUrl(app.Image.EditIconVirtPath);
						  ItemLockAlt = "Opened by Me";
					 }
					 else
					 {
						  ItemLockSrc = app.Image.LockIconVirtPath;
						  ItemLockAlt = String.Format("Locked by {0}", user.Name);
						  
					 }
					 FooterClass += " itemLocked";
				}
		  }
	 }
}
