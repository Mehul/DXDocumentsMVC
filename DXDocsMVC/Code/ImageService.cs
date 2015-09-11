using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Drawing;
using System.Drawing.Drawing2D;

using DevExpress.XtraRichEdit;
using DevExpress.XtraPrintingLinks;
using DevExpress.XtraPrinting;

using DevExpress.Spreadsheet;
using DevExpress.XtraSpreadsheet.Services;
using DevExpress.XtraSpreadsheet.Services.Implementation;

using DevExpress.Web;
namespace DXDocsMVC.Code
{

	 public class ImageService : ServiceBase
	 {
		  static readonly TimeSpan FirstThumbnailUpdatingInterval = TimeSpan.FromSeconds(10);
		  static readonly TimeSpan GeneralThumbnailUpdatingInterval = TimeSpan.FromMinutes(1);

		  bool ThumbnailUpdatingEnabled { get; set; }
		  public int ThumbnailWidth { get { return ThumbnailService.ThumbnailWidth; } }
		  public int ThumbnailHeight { get { return ThumbnailService.ThumbnailHeight; } }
		  public string LockIconVirtPath { get { return ThumbnailService.LockIconVirtPath; } }
		  public string EditIconVirtPath { get { return ThumbnailService.EditIconVirtPath; } }

		  DXDocsMVC.Code.DataService DataService { get { return DocumentsApp.Data; } }
		  DocumentService DocumentService { get { return DocumentsApp.Document; } }

		  protected AvatarService AvatarService { get; set; }
		  protected ThumbnailService ThumbnailService { get; set; }

		  public ImageService(DocumentsApp app)
				: base(app)
		  {
				ThumbnailUpdatingEnabled = false;
				AvatarService = new AvatarService(app);
				ThumbnailService = new ThumbnailService(app);
		  }

		  public string GetFolderIconUrl()
		  {
				return ThumbnailService.GetFolderIconUrl();
		  }
		  public string GetAvatarVirtPath(User user)
		  {
				return AvatarService.GetAvatarVirtPath(user);
		  }
		  public string GetIconVirtPath(Item item)
		  {
				return ThumbnailService.GetIconVirtPath(item);
		  }
		  public string GetThumbnailUrl(Item item)
		  {
				return ThumbnailService.GetThumbnailUrl(item);
		  }
		  public bool HasPreview(Item item)
		  {
				return ThumbnailService.HasPreview(item);
		  }
		  public string GetIconUrl(Item item)
		  {
				return ThumbnailService.GetIconUrl(item);
		  }
		  public string GetFooterItemIconVirtPath(Item item)
		  {
				return ThumbnailService.GetFooterItemIconVirtPath(item);
		  }
		  public string GetDetailsViewItemIcon(Item item)
		  {
				return ThumbnailService.GetDetailsViewItemIcon(item);
		  }
		  public void UpdateThumbnails()
		  {
				ThumbnailService.UpdateThumbnails();
		  }
		  public void EnableThumbnailUpdating()
		  {
				ThumbnailUpdatingEnabled = true;
				UpdateThumbnailsWithDelaty(FirstThumbnailUpdatingInterval);
		  }
		  public void DisableThumbnailUpdating()
		  {
				ThumbnailUpdatingEnabled = false;
		  }
		  void UpdateThumbnailsWithDelaty(TimeSpan delay)
		  {
				CommonUtils.ExecuteActionWithDelay(() => OnDelayedThumbnailUpdating(), delay);
		  }
		  void OnDelayedThumbnailUpdating()
		  {
				if (!ThumbnailUpdatingEnabled)
					 return;

				try
				{
					 UpdateThumbnails();
				}
				finally
				{
					 UpdateThumbnailsWithDelaty(GeneralThumbnailUpdatingInterval);
					 DataService.CloseUnitOfWork();
				}
		  }
	 }

	 public class AvatarService : ServiceBase
	 {
		  const string AvatarFolderVirtPath = @"~\Content\Images\Avatars";

		  DXDocsMVC.Code.DataService DataService { get { return DocumentsApp.Data; } }
		  DocumentService DocumentService { get { return DocumentsApp.Document; } }

		  object loadingLocker = new object();
		  string avatarFolderPath;
		  bool IsLoaded { get; set; }
		  HttpContext Context { get { return HttpContext.Current; } }
		  string AvatarFolderPath
		  {
				get
				{
					 if (string.IsNullOrEmpty(avatarFolderPath))
						  this.avatarFolderPath = Context.Server.MapPath(GetAvatarFolderVirtPath());
					 return this.avatarFolderPath;
				}
		  }

		  public AvatarService(DocumentsApp app) : base(app) { }

		  public string GetAvatarVirtPath(User user)
		  {
				EnsureAvatarsLoaded();
				return Path.Combine(GetAvatarFolderVirtPath(), GetAvatarName(user));
		  }
		  protected virtual string GetAvatarFolderVirtPath()
		  {
				return AvatarFolderVirtPath;
		  }
		  string GetAvatarName(User user)
		  {
				return user.Id.ToString() + ".jpg";
		  }
		  void EnsureAvatarsLoaded()
		  {
				lock (this.loadingLocker)
				{
					 if (!IsLoaded)
					 {
						  LoadAvatars();
						  IsLoaded = true;
					 }
				}
		  }
		  void LoadAvatars()
		  {
				if (!Directory.Exists(AvatarFolderPath))
					 Directory.CreateDirectory(AvatarFolderPath);
				IEnumerable<User> users = DataService.Users.ToList();
				foreach (User user in users)
					 LoadAvatar(user);
		  }
		  void LoadAvatar(User user)
		  {
				if (user.Avatar != null)
				{
					 string avatarPath = Path.Combine(AvatarFolderPath, GetAvatarName(user));
					 File.WriteAllBytes(avatarPath, user.Avatar.Data);
				}
		  }
	 }

	 public class ThumbnailService : ServiceBase
	 {
		  const string ThumbnailFolderVirtPath = @"~\Content\Images\Thumb";
		  const string PredefinedImagesFolderVirtPath = @"~\Content\Images\Predefined";
		  const string IconsFolderVirtPath = @"~\Content\Images\Icons";
		  const string DetailsIconsFolderVirtPath = @"~\Content\Images\Icons\Details";
		  const string EmptyThumbnailName = "EmptyPreview.png";
		  const string FolderIconName = "Folder.png";
		  const string LockIconName = "Lock.png";
		  const string EditIconName = "Edit.png";
		  public const int ThumbnailWidth = 240;
		  public const int ThumbnailHeight = 195;

		  object updateLocker = new object();

		  DXDocsMVC.Code.DataService DataService { get { return DocumentsApp.Data; } }
		  DocumentService DocumentService { get { return DocumentsApp.Document; } }

		  string[] ImageExtensions { get; set; }
		  string EmptyThumbnailVirtPath
		  {
				get { return Path.Combine(PredefinedImagesFolderVirtPath, EmptyThumbnailName); }
		  }

		  public string LockIconVirtPath
		  {
				get { return Path.Combine(IconsFolderVirtPath, LockIconName); }
		  }
		  public string EditIconVirtPath
		  {
				get { return Path.Combine(IconsFolderVirtPath, EditIconName); }
		  }

		  public ThumbnailService(DocumentsApp app)
				: base(app)
		  {
				ImageExtensions = new string[] { ".png", ".gif", ".jpg", ".jpeg", ".ico", ".bmp" };
		  }

		  public string GetThumbnailUrl(Item item)
		  {
				string thumbVirtPath = HasPreview(item)
					 ? GetThumbnailVirtPathInternal(item)
					 : GetIconVirtPath(item);
				return string.IsNullOrEmpty(thumbVirtPath) ? null : CommonUtils.ConvertVirtPathToUrl(thumbVirtPath);
		  }
		  public bool HasPreview(Item item)
		  {
				string thumbVirtPath = GetThumbnailVirtPathInternal(item);
				string thumbPath = CommonUtils.MapPath(thumbVirtPath);
				return File.Exists(thumbPath);
		  }
		  public string GetIconUrl(Item item)
		  {
				string iconVirtPath = GetIconVirtPath(item);
				return string.IsNullOrEmpty(iconVirtPath) ? null : CommonUtils.ConvertVirtPathToUrl(iconVirtPath);
		  }
		  public string GetIconVirtPath(Item item)
		  {
				string iconName = GetIconName(item);
				return string.IsNullOrEmpty(iconName) ? null : Path.Combine(PredefinedImagesFolderVirtPath, iconName);
		  }
		  public string GetFooterItemIconVirtPath(Item item)
		  {
				string iconName = DocumentService.GetIconName(item);
				return string.IsNullOrEmpty(iconName) ? null : Path.Combine(IconsFolderVirtPath, iconName);
		  }
		  public string GetDetailsViewItemIcon(Item item)
		  {
				string iconName = GetIconName(item);
				return string.IsNullOrEmpty(iconName) ? null : Path.Combine(DetailsIconsFolderVirtPath, iconName);
		  }
		  public string GetFolderIconVirtPath()
		  {
				return Path.Combine(PredefinedImagesFolderVirtPath, FolderIconName);
		  }
		  public string GetFolderIconUrl()
		  {
				return CommonUtils.ConvertVirtPathToUrl(GetFolderIconVirtPath());
		  }
		  string GetIconName(Item item)
		  {
				if (item.IsFolder)
					 return FolderIconName;
				string iconName = DocumentService.GetIconName(item);
				if (string.IsNullOrEmpty(iconName))
					 iconName = GetIconNameInternal(item);
				return iconName;
		  }
		  string GetIconNameInternal(Item item)
		  {
				string extension = Path.GetExtension(item.Name);
				if (Array.Exists(ImageExtensions, str => str.Equals(extension, StringComparison.OrdinalIgnoreCase)))
					 return null;
				switch (extension)
				{
					 case ".avi":
						  return "Video.png";
					 case ".mp3":
						  return "Audio.png";
				}
				return "Generic.png";
		  }
		  string GetThumbnailName(Item item)
		  {
				return string.Format("{0}_{1}.png", item.Name, item.Id.ToString());
		  }
		  string GetThumbnailVirtPathInternal(Item item)
		  {
				string thumbName = item.Id.ToString() + ".png";
				return Path.Combine(GetThumbnailFolderVirtPath(), thumbName);
		  }
		  protected virtual string GetThumbnailFolderVirtPath()
		  {
				return ThumbnailFolderVirtPath;
		  }

		  public void UpdateThumbnails()
		  {
				lock (this.updateLocker)
				{
					 string thumbFolderPath = CommonUtils.MapPath(GetThumbnailFolderVirtPath());
					 if (!Directory.Exists(thumbFolderPath))
						  Directory.CreateDirectory(thumbFolderPath);
					 List<Item> fileItems = DataService.Items.Where(item => !item.IsFolder).ToList();
					 foreach (Item fileItem in fileItems)
					 {
						  try
						  {
								GenerateThumbnail(fileItem);
						  }
						  catch { }
					 }

				}
		  }
		  void GenerateThumbnail(Item item)
		  {
				string thumbPath = GetThumbnailPath(item);
				FileInfo thumbFile = new FileInfo(thumbPath);
				if (!thumbFile.Exists || item.LastWriteTime >= thumbFile.LastWriteTimeUtc)
					 GenerateThumbnailCore(item);
		  }
		  void GenerateThumbnailCore(Item item)
		  {
				IPrintable component = DocumentService.CreatePrintableComponent(item);
				if (component == null)
					 return;

				PrintableComponentLinkBase pcl = new PrintableComponentLinkBase(new PrintingSystemBase());
				pcl.Component = component;
				pcl.CreateDocument();
				ImageExportOptions imgOptions = new ImageExportOptions();
				imgOptions.ExportMode = ImageExportMode.SingleFilePageByPage;
				imgOptions.Format = System.Drawing.Imaging.ImageFormat.Png;
				imgOptions.PageRange = "1";
				imgOptions.PageBorderWidth = 0;
				MemoryStream stream = new MemoryStream();
				pcl.ExportToImage(stream, imgOptions);

				stream.Position = 0;
				string thumbPath = GetThumbnailPath(item);
				GenerateThumbnailInternal(stream, thumbPath, ThumbnailWidth, ThumbnailHeight);

				IDisposable disposableComponent = component as IDisposable;
				if (disposableComponent != null)
					 disposableComponent.Dispose();
		  }
		  string GetThumbnailPath(Item item)
		  {
				string thumbVirtPath = GetThumbnailVirtPathInternal(item);
				return CommonUtils.MapPath(thumbVirtPath);
		  }


		  void GenerateThumbnailInternal(Stream file, string thumbnailPath, int width, int height)
		  {
				System.Drawing.Image original = System.Drawing.Image.FromStream(file);
				System.Drawing.Bitmap thumbnail = ChangeImageSize(original, width, height);
				try
				{
					 thumbnail.Save(thumbnailPath);
				}
				catch
				{
					 throw;
				}
				finally
				{
					 thumbnail.Dispose();
					 original.Dispose();
				}
		  }
		  Bitmap ChangeImageSize(Image original, int width, int height)
		  {
				float scaleThumbnail = (float)height / (float)width;

				RectangleF srcRect = new RectangleF(0.0F, 0.0F, original.Width, original.Width * scaleThumbnail);
				RectangleF dstRect = new RectangleF(0.0F, 0.0F, width, height);

				Bitmap thumbnail = new Bitmap(width, height);
				Graphics g = Graphics.FromImage(thumbnail);

				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.DrawImage(original, dstRect, srcRect, GraphicsUnit.Pixel);

				return thumbnail;
		  }
	 }
}