using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DXDocsMVC.Code
{

	 public class DocumentsApp
	 {
		  static DocumentsApp instance { get; set; }

		  public static DocumentsApp Instance { get { return GetInstance(); } }

		  static DocumentsApp GetInstance()
		  {
				if (DemoUtils.IsSiteMode)
					 return DemoUtils.GetSiteModeAppInstance();
				else
				{
					 if (instance == null)
					 {
						  instance = new DocumentsApp();
						  instance.Initialize();
					 }
					 return instance;
				}
		  }

		  public DXDocsMVC.Code.DataService Data { get; protected set; }
		  public FileSystemService FileSystem { get; protected set; }
		  public DocumentService Document { get; protected set; }
		  public UserService User { get; protected set; }
		  public ImageService Image { get; protected set; }

		  public DocumentsApp() { }

		  public virtual void Start()
		  {
				Data.Initialize();
				Image.EnableThumbnailUpdating();
				User.CheckActivityWithDelay();
				Document.SaveAllDocumentsWithDelaty();
		  }
		  public virtual void End()
		  {
				Document.SaveAllDocuments();
				Data.CloseUnitOfWork();
		  }
		  public string GetCurrentUserAvatarVirtPath()
		  {
				return Image.GetAvatarVirtPath(User.CurrentUser);
		  }
		  public bool TryLockDocument(Item item)
		  {
				return Document.TryLockDocument(item, User.CurrentUser);
		  }

		  public virtual void Initialize()
		  {
				Data = new DXDocsMVC.Code.EntityDataService(this);
				FileSystem = new FileSystemService(this);
				Document = new DocumentService(this);
				User = new UserService(this);
				Image = new ImageService(this);
		  }
	 }

	 public class ServiceBase
	 {
		  public DocumentsApp DocumentsApp { get; private set; }

		  public ServiceBase(DocumentsApp app)
		  {
				DocumentsApp = app;
		  }
	 }
}