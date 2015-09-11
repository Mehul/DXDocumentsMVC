using DevExpress.Web;
using DevExpress.Web.Mvc;
using DXDocsMVC.Code;
using DXDocsMVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DXDocsMVC.Controllers
{	 
	 public class HomeController : Controller
    {
		  static string SelectedFilterText(string selectedFilter, string defaultText) {
            switch(selectedFilter) {
                case "My":
                    return "My documents";
                case "Recent":
                    return "Recent documents";
                case "RTFDocs":
                    return "Rich text documents";
                case "Sheets":
                    return "Worksheet documents";
					 default:
						  return defaultText;
            }
            
        }
    

        // GET: Default
		  [Authorize]
		  public ActionResult Index()
        {
				var app = DocumentsApp.Instance;
				string currentFilter = "All";
				var model = new HomeModel()
            {
					 DocumentsApp = app,
					 FileSystemProvider = CreateFileSystemProvider(app, currentFilter),
					 IsFilterApplied = currentFilter != "All",
					 PanelName = SelectedFilterText(currentFilter, "All documents"),
					 SourceName = currentFilter,
					 FilterName = currentFilter
				};
            return View(model);
        }

		  [Authorize]
		  [HttpPost]
		  public ActionResult SignOut(string signout)
		  {
				DocumentsApp.Instance.User.SignOut();
				return RedirectToAction("Index");
		  }

		  public ActionResult CallbackPanelPartial(string filtered)
		  {
				var app = DocumentsApp.Instance;
				string currentFilter = filtered??"All";
				//string currentView = view ?? "Thumbnails";
				var model = new HomeModel()
            {
					 DocumentsApp = app,
					 FileSystemProvider = CreateFileSystemProvider(app, currentFilter) ,
					 IsFilterApplied = currentFilter != "All",
					 PanelName = SelectedFilterText(currentFilter, "FilterNameLabel"),
					 SourceName =currentFilter,
					 FilterName =currentFilter
            };
				
				return PartialView("_CallbackPanelPartial", model );
		  }

		  protected static FileSystemProviderBase CreateFileSystemProvider(DocumentsApp app, string selectedFilter)
		  {
				if (selectedFilter != "All")
				{
					 FilteredFileSystemProvider queryProvider = new FilteredFileSystemProvider(app, "")
					 {
						  FileQuery = app.FileSystem.GetFilesFromSource(selectedFilter)
					 };
					 return queryProvider;
				}
				return new GeneralFileSystemProvider(app, "Files");
		  }
		  
		  [ValidateInput(false)]
		  public ActionResult FileManagerPartial(string filtered)
		  {
				var app = DocumentsApp.Instance;
				string currentFilter = filtered ?? "All";
				var model = new HomeModel()
				{
					 DocumentsApp = app,
					 FileSystemProvider = CreateFileSystemProvider(app, currentFilter),
					 IsFilterApplied = currentFilter != "All",
					 PanelName = SelectedFilterText(currentFilter, "FilterNameLabel"),
					 SourceName = currentFilter,
					 FilterName = currentFilter
				};
				return PartialView("_FileManagerPartial", model /*HomeControllerFileManagerSettings.Model*/);
		  }
		  
		  
		  public FileStreamResult FileManagerPartialDownload(string filtered)
		  {
				var app = DocumentsApp.Instance;
				string currentFilter = filtered ?? "All";
				var model = new HomeModel()
				{
					 DocumentsApp = app,
					 FileSystemProvider = CreateFileSystemProvider(app, currentFilter),
					 IsFilterApplied = currentFilter != "All",
					 PanelName = SelectedFilterText(currentFilter, "FilterNameLabel"),
					 SourceName = currentFilter,
					 FilterName = currentFilter
				};
				return FileManagerExtension.DownloadFiles("FileManager", model.FileSystemProvider /*HomeControllerFileManagerSettings.Model*/);
		  }

	
    }
}