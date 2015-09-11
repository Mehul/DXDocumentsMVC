using DevExpress.Utils;
using DevExpress.Web;
using DevExpress.Web.Mvc;
using DXDocsMVC.Code;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace DXDocsMVC.Models
{
	 public class HomeModel
	 {
		  #region Read-Only properties

		  public string CurrentViewModeName { get { return DocumentsApp.User.CurrentViewModeName; } }		  
		  public bool IsDetailsViewMode { get { return CurrentViewModeName == "Details"; } }
		  public FileListView CurrentViewMode { get { return IsDetailsViewMode ? FileListView.Details : FileListView.Thumbnails; } }

		  public MVCxMenuItem[] ActionMenuItems { get { return UtilsMvc.ActionMenuItems; } }
		  public MVCxMenuItem[] FilterMenuItems { get { return UtilsMvc.FilterMenuItems; } }
		  
		  public MVCxMenuItem[] UserMenuItems { get { return UtilsMvc.GetUserMenuItems(DocumentsApp.User.CurrentUser.Name, DocumentsApp.GetCurrentUserAvatarVirtPath()); } }

		  //public string FileManagerID { get { return "FileManager"; /*(IsFilterApplied ? "Filtered" : "") + "FileManager";*/ } }
		  public string FileManagerCssClass { get { return String.Format("FileManager{0}", IsDetailsViewMode ? " DetailsView" : " ThumbnailsView"); } }
		  public bool FileManagerUploadEnabled { get { return !IsFilterApplied; } }
		  public bool FileManagerUploadShowUploadPanel { get { return IsFilterApplied; } }
		  public long FileManagerUploadMaxFileSize { get { return DocumentsApp.FileSystem.GetMaxFileSizeForUpload(); } }
		  public bool FileManagerBreadCrumbsVisible { get { return !IsFilterApplied; } }

		  public string FileManagerFileAreaFolderUrl { get { return DocumentsApp.Image.GetFolderIconUrl(); } }

		  public Unit FileManagerThumbnailWidth { get { return Unit.Pixel(DocumentsApp.Image.ThumbnailWidth); } }
		  public Unit FileManagerThumbnailHeight { get { return Unit.Pixel(DocumentsApp.Image.ThumbnailHeight); } }
		  //public Unit FileManagerCommandColumnWidth { get { return Unit.Pixel(80); } }

		  public Unit FileManagerFileWidth { get { return IsDetailsViewMode ? Unit.Empty : Unit.Pixel(270); } }
		  public Unit FileManagerFileHeight { get { return IsDetailsViewMode ? Unit.Empty : Unit.Pixel(270); } }

		  public bool FileManagerBrowserPanelVisible { get { return IsFilterApplied; } }
		  public string BrowserPanelCssClass { get { return String.Format("BrowserPanel{0}", IsFilterApplied ? " ShowPanel" : ""); } }

		  public string UserName { get { return DocumentsApp.User.CurrentUser.Name ;} }
		  
		  #endregion
		  
		  public DocumentsApp DocumentsApp { get; set; }
		  public FileSystemProviderBase FileSystemProvider { get; set; }
		  public bool IsFilterApplied { get; set; }
		  public string PanelName { get; set; }
		  public string SourceName { get; set; }
		  public string FilterName { get; set; }		  
	 }
}
