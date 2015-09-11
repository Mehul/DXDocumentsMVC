using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.Caching;
using System.Web.UI;
using DevExpress.Web;
using DevExpress.Web.Office;
using DevExpress.Web.Internal;
namespace DXDocsMVC.Code
{

	 public class DemoUtils
	 {
		  static bool? isSiteMode;

		  public static bool IsSiteMode
		  {
				get
				{
					 if (!isSiteMode.HasValue)
					 {
						  string siteModeValue = System.Web.Configuration.WebConfigurationManager.AppSettings["SiteMode"];
						  isSiteMode = siteModeValue.Equals("true", StringComparison.InvariantCultureIgnoreCase);
					 }
					 return isSiteMode.Value;
				}
		  }

		  public static DocumentsApp GetSiteModeAppInstance()
		  {
				return SiteModeUtils.GetAppInstance();
		  }

		  public static void IgnoreDemoClasses(System.Data.Entity.DbModelBuilder modelBuilder)
		  {
				DemoDataService.IgnoreDemoClasses(modelBuilder);
		  }
	 }

	 public class DocumentsDemoApp : DocumentsApp
	 {
		  public string Id { get; private set; }

		  public DocumentsDemoApp(string id)
				: base()
		  {
				Id = id;
		  }

		  public override void Initialize()
		  {
				base.Initialize();
				Data = new DemoDataService(this, Data);
				FileSystem = new DemoFileSystemService(this);
				Document = new DemoDocumentService(this);
				Image = new DemoImageService(this);
		  }

		  public void Dispose()
		  {
				(Data as DemoDataService).Dispose();
				(Image as DemoImageService).Dispose();
				(Document as DemoDocumentService).Dispose();
		  }
	 }

	 public class DemoItem : Item
	 {
		  DemoDataService DataService { get; set; }

		  public long? ContentId { get; set; }
		  public long? OwnerId { get; set; }
		  public long? ParentItemId { get; set; }

		  public DemoItem(DemoDataService dataService)
		  {
				DataService = dataService;
		  }
	 }

	 public class DemoUser : User
	 {
		  DemoDataService DataService { get; set; }

		  public long? AvatarId { get; set; }

		  public DemoUser(DemoDataService dataService)
		  {
				DataService = dataService;
		  }
	 }

	 public class DemoBinaryContent : BinaryContent
	 {
		  DemoDataService DataService { get; set; }
		  public bool IsLoaded { get; set; }

		  public DemoBinaryContent(DemoDataService dataService)
		  {
				DataService = dataService;
				IsLoaded = false;
		  }

		  public override byte[] Data
		  {
				get
				{
					 return DataService.ReadContentBytes(this);
				}
				set
				{
					 DataService.WriteContentBytes(this, value);
				}
		  }
	 }

	 public class DemoDataService : DXDocsMVC.Code.DataService
	 {
		  const string DemoDataRootVirtPath = @"~\App_Data\DemoData";

		  static bool IsInitialized { get; set; }

		  static DemoDataService()
		  {
				IsInitialized = false;
		  }

		  public static void IgnoreDemoClasses(System.Data.Entity.DbModelBuilder modelBuilder)
		  {
				modelBuilder.Ignore<DemoItem>();
				modelBuilder.Ignore<DemoUser>();
				modelBuilder.Ignore<DemoBinaryContent>();
		  }

		  object dataLoadingLocker = new object();
		  object itemCacheLocker = new object();
		  object userCacheLocker = new object();
		  object binaryContentCacheLocker = new object();
		  object demoDataLocker = new object();

		  DocumentsDemoApp DemoApp { get { return DocumentsApp as DocumentsDemoApp; } }
		  DXDocsMVC.Code.DataService SourceDataService { get; set; }

		  bool IsDataLoaded { get; set; }
		  long MaxItemId { get; set; }
		  long MaxBinaryContentId { get; set; }
		  Dictionary<long, DemoUser> UserCache { get; set; }
		  Dictionary<long, DemoItem> ItemCache { get; set; }
		  Dictionary<long, DemoBinaryContent> BinaryContentCache { get; set; }

		  public override IQueryable<Item> Items
		  {
				get
				{
					 EnsureDataLoaded();
					 lock (this.itemCacheLocker)
					 {
						  return ItemCache.Values.ToList().AsQueryable();
					 }
				}
		  }
		  public override IQueryable<User> Users
		  {
				get
				{
					 EnsureDataLoaded();
					 lock (this.userCacheLocker)
					 {
						  return UserCache.Values.ToList().AsQueryable();
					 }
				}
		  }
		  public override IQueryable<BinaryContent> BinaryContentSet
		  {
				get { return new BinaryContent[0].AsQueryable(); }
		  }

		  public DemoDataService(DocumentsDemoApp app, DXDocsMVC.Code.DataService srcDataService)
				: base(app)
		  {
				SourceDataService = srcDataService;
		  }

		  public override void SaveChanges() { }
		  public override void CloseUnitOfWork()
		  {
				SourceDataService.CloseUnitOfWork();
		  }
		  public override User FindUserByAccountName(string accountName)
		  {
				return Users.FirstOrDefault(user => user.AccountName == accountName);
		  }
		  public override User FindUserById(long id)
		  {
				return Users.FirstOrDefault(user => user.Id == id);
		  }
		  public override User CreateUser()
		  {
				return null;
		  }
		  public override void DeleteUser(long id) { }

		  public override Item FindItemById(long id)
		  {
				lock (this.itemCacheLocker)
				{
					 DemoItem item;
					 if (ItemCache.TryGetValue(id, out item))
						  return item;
					 return null;
				}
		  }
		  public override void DeleteItem(Item item)
		  {
				lock (this.itemCacheLocker)
				{
					 DeleteItemCore(item as DemoItem);
				}
		  }
		  public override void AddItem(Item item)
		  {
				DemoItem demoItem = new DemoItem(this)
				{
					 Name = item.Name,
					 IsFolder = item.IsFolder,
					 Owner = item.Owner,
					 OwnerId = item.Owner == null ? (long?)null : item.Owner.Id,
					 ParentItem = item.ParentItem,
					 ParentItemId = item.ParentItem == null ? (long?)null : item.ParentItem.Id,
					 Content = item.Content,
					 ContentId = item.Content == null ? (long?)null : item.Content.Id,
					 CreationTime = item.CreationTime,
					 LastAccessTime = item.LastAccessTime,
					 LastWriteTime = item.LastWriteTime
				};
				lock (this.itemCacheLocker)
				{
					 MaxItemId++;
					 demoItem.Id = MaxItemId;
					 ItemCache.Add(demoItem.Id, demoItem);
				}
				item.Id = demoItem.Id;
		  }

		  public override BinaryContent CreateBinaryContent(byte[] data)
		  {
				DemoBinaryContent content = new DemoBinaryContent(this);
				lock (this.binaryContentCacheLocker)
				{
					 MaxBinaryContentId++;
					 content.Id = MaxBinaryContentId;
					 BinaryContentCache.Add(content.Id, content);
				}
				content.Data = data;
				return content;
		  }
		  public override BinaryContent CreateBinaryContent(Stream dataStream)
		  {
				byte[] data = CommonUtils.ReadAllBytes(dataStream);
				return CreateBinaryContent(data);
		  }

		  public byte[] ReadContentBytes(DemoBinaryContent content)
		  {
				if (content.IsLoaded)
				{
					 string filePath = PrepareContentFilePath(content);
					 lock (this.demoDataLocker)
					 {
						  return File.ReadAllBytes(filePath);
					 }
				}
				byte[] data = SourceDataService.BinaryContentSet.
					 Where(dbContent => dbContent.Id == content.Id).
					 Select(dbContent => dbContent.Data).
					 First();
				WriteContentBytes(content, data);
				return data;
		  }
		  public void WriteContentBytes(DemoBinaryContent content, byte[] data)
		  {
				string filePath = PrepareContentFilePath(content);
				lock (this.demoDataLocker)
				{
					 File.WriteAllBytes(filePath, data);
				}
				content.IsLoaded = true;
		  }

		  public override Stream ReadFileContent(Item fileItem)
		  {
				return new MemoryStream(fileItem.Content.Data);
		  }
		  public override int GetFileSize(Item fileItem)
		  {
				DemoItem item = fileItem as DemoItem;
				if (item.Content != null)
				{
					 DemoBinaryContent content = item.Content as DemoBinaryContent;
					 if (content.IsLoaded)
					 {
						  string filePath = PrepareContentFilePath(content);
						  lock (this.demoDataLocker)
						  {
								FileInfo file = new FileInfo(filePath);
								return (int)file.Length;
						  }
					 }
					 return SourceDataService.GetFileSize(fileItem);
				}
				return 0;
		  }

		  public override void Initialize()
		  {
				if (!IsInitialized)
				{
					 SourceDataService.Initialize();
					 IsInitialized = true;
				}
		  }

		  public void Dispose()
		  {
				SiteModeUtils.SafeDeleteDirectory(GetDemoDataPath());
		  }

		  void EnsureDataLoaded()
		  {
				lock (this.dataLoadingLocker)
				{
					 if (!IsDataLoaded)
					 {
						  LoadData();
						  IsDataLoaded = true;
					 }
				}
		  }
		  void LoadData()
		  {
				MaxItemId = 0;
				MaxBinaryContentId = 0;
				BinaryContentCache = new Dictionary<long, DemoBinaryContent>();
				LoadUsers();
				LoadItems();
		  }
		  void LoadUsers()
		  {
				UserCache = SourceDataService.Users.
					 Join(SourceDataService.BinaryContentSet, dbUser => dbUser.Avatar.Id, dbContent => dbContent.Id,
						  (dbUser, dbContent) => new
						  {
								Id = dbUser.Id,
								AccountName = dbUser.AccountName,
								Name = dbUser.Name,
								Password = dbUser.Password,
								AvatarId = dbContent.Id,
								AvatarData = dbContent.Data
						  }).
					 ToList().
					 Select(userDto => new DemoUser(this)
					 {
						  Id = userDto.Id,
						  AccountName = userDto.AccountName,
						  Name = userDto.Name,
						  Password = userDto.Password,
						  AvatarId = userDto.AvatarId,
						  Avatar = CreateInitialBinaryContent(userDto.AvatarId, userDto.AvatarData)
					 }).
					 ToDictionary(user => user.Id);
		  }
		  void LoadItems()
		  {
				ItemCache = SourceDataService.Items.
					 Select(dbItem => new
					 {
						  Id = dbItem.Id,
						  Name = dbItem.Name,
						  IsFolder = dbItem.IsFolder,
						  ContentId = (long?)dbItem.Content.Id,
						  OwnerId = (long?)dbItem.Owner.Id,
						  ParentItemId = (long?)dbItem.ParentItem.Id,
						  CreationTime = dbItem.CreationTime,
						  LastAccessTime = dbItem.LastAccessTime,
						  LastWriteTime = dbItem.LastWriteTime
					 }).
					 ToList().
					 Select(itemDto => new DemoItem(this)
					 {
						  Id = itemDto.Id,
						  Name = itemDto.Name,
						  IsFolder = itemDto.IsFolder,
						  ContentId = itemDto.ContentId,
						  OwnerId = itemDto.OwnerId,
						  ParentItemId = itemDto.ParentItemId,
						  CreationTime = itemDto.CreationTime,
						  LastAccessTime = itemDto.LastAccessTime,
						  LastWriteTime = itemDto.LastWriteTime
					 }).
					 ToDictionary(item => item.Id);

				List<DemoItem> items = ItemCache.Values.ToList();
				foreach (DemoItem item in items)
				{
					 if (MaxItemId < item.Id)
						  MaxItemId = item.Id;
					 if (item.OwnerId.HasValue)
						  item.Owner = UserCache[item.OwnerId.Value];
					 if (item.ParentItemId.HasValue)
						  item.ParentItem = ItemCache[item.ParentItemId.Value];
					 if (item.ContentId.HasValue)
						  item.Content = CreateInitialBinaryContent(item.ContentId.Value);
				}
		  }
		  DemoBinaryContent CreateInitialBinaryContent(long id)
		  {
				return CreateInitialBinaryContent(id, null);
		  }
		  DemoBinaryContent CreateInitialBinaryContent(long id, byte[] data)
		  {
				DemoBinaryContent content = new DemoBinaryContent(this);
				content.Id = id;
				if (data != null)
					 content.Data = data;
				BinaryContentCache.Add(id, content);
				if (MaxBinaryContentId < id)
					 MaxBinaryContentId = id;
				return content;
		  }
		  void DeleteItemCore(DemoItem item)
		  {
				ItemCache.Remove(item.Id);
				if (item.IsFolder)
				{
					 List<DemoItem> childItems = ItemCache.Values.
						  Where(cacheItem => !cacheItem.IsRoot && cacheItem.ParentItem.Id == item.Id).
						  ToList();

					 foreach (DemoItem childItem in childItems)
						  DeleteItemCore(childItem);
				}
		  }
		  string GetDemoDataPath()
		  {
				string demoDataVirtPath = Path.Combine(DemoDataRootVirtPath, DemoApp.Id);
				return CommonUtils.MapPath(demoDataVirtPath);
		  }
		  string GetBinaryContentFileName(DemoBinaryContent content)
		  {
				return content.Id.ToString() + ".tmp";
		  }
		  string PrepareContentFilePath(DemoBinaryContent content)
		  {
				string demoDataPath = GetDemoDataPath();
				if (!Directory.Exists(demoDataPath))
					 Directory.CreateDirectory(demoDataPath);

				string fileName = GetBinaryContentFileName(content);
				return Path.Combine(demoDataPath, fileName);
		  }
	 }

	 public class DemoFileSystemService : FileSystemService
	 {
		  const long MaxFileSizeForUpload = 1048576;

		  public DemoFileSystemService(DocumentsDemoApp app) : base(app) { }

		  public override Item CreateNewFileItem(string folderPath, string fileName)
		  {
				Item item = base.CreateNewFileItem(folderPath, fileName);
				return DocumentsApp.Data.FindItemById(item.Id);
		  }
		  public override long GetMaxFileSizeForUpload()
		  {
				return MaxFileSizeForUpload;
		  }
	 }

	 public class DemoDocumentService : DocumentService
	 {
		  public DemoDocumentService(DocumentsDemoApp app) : base(app) { }

		  public override string GetDocumentIdForEditor(Item document)
		  {
				string appId = (DocumentsApp as DocumentsDemoApp).Id;
				return base.GetDocumentIdForEditor(document) + "#" + appId;
		  }
		  protected override long ParseDocumentIdFromEditor(string editorDocId)
		  {
				string appId = (DocumentsApp as DocumentsDemoApp).Id;
				if (!DocumentIdBelongsToCurrentApp(editorDocId))
					 return -1;
				int delimeterIndex = editorDocId.IndexOf('#');
				string baseDocId = editorDocId.Substring(0, delimeterIndex);
				return base.ParseDocumentIdFromEditor(baseDocId);
		  }

		  public void Dispose()
		  {
				DisableDocumentAutoSave();
				CloseAllDocuments();
		  }
		  bool DocumentIdBelongsToCurrentApp(string editorDocId)
		  {
				string appId = (DocumentsApp as DocumentsDemoApp).Id;
				return editorDocId.IndexOf(appId) != -1;
		  }
		  void CloseAllDocuments()
		  {
				string appId = (DocumentsApp as DocumentsDemoApp).Id;
				List<IDocumentInfo> documents = DocumentManager.GetAllDocuments().ToList();
				foreach (IDocumentInfo document in documents)
				{
					 try
					 {
						  if (DocumentIdBelongsToCurrentApp(document.DocumentId))
								document.Close();
					 }
					 catch { }
				}
		  }
	 }

	 public class DemoImageService : ImageService
	 {
		  public DemoImageService(DocumentsDemoApp app)
				: base(app)
		  {
				AvatarService = new DemoAvatarService(app);
				ThumbnailService = new DemoThumbnailService(app);
		  }

		  public void Dispose()
		  {
				DisableThumbnailUpdating();
				(AvatarService as DemoAvatarService).Dispose();
				(ThumbnailService as DemoThumbnailService).Dispose();
		  }
	 }

	 public class DemoAvatarService : AvatarService
	 {
		  public DemoAvatarService(DocumentsDemoApp app) : base(app) { }

		  protected override string GetAvatarFolderVirtPath()
		  {
				string appId = (DocumentsApp as DocumentsDemoApp).Id;
				return Path.Combine(base.GetAvatarFolderVirtPath(), appId);
		  }

		  public void Dispose()
		  {
				string avatarFolderPath = CommonUtils.MapPath(GetAvatarFolderVirtPath());
				SiteModeUtils.SafeDeleteDirectory(avatarFolderPath);
		  }
	 }

	 public class DemoThumbnailService : ThumbnailService
	 {
		  public DemoThumbnailService(DocumentsDemoApp app) : base(app) { }

		  protected override string GetThumbnailFolderVirtPath()
		  {
				string appId = (DocumentsApp as DocumentsDemoApp).Id;
				return Path.Combine(base.GetThumbnailFolderVirtPath(), appId);
		  }

		  public void Dispose()
		  {
				string thumbnailFolderPath = CommonUtils.MapPath(GetThumbnailFolderVirtPath());
				SiteModeUtils.SafeDeleteDirectory(thumbnailFolderPath);
		  }
	 }

	 public class DemoAppRegistryEntry
	 {
		  public DocumentsDemoApp DemoApp { get; set; }
		  public DateTime LastAccessTime { get; set; }

		  public DemoAppRegistryEntry(DocumentsDemoApp app)
		  {
				DemoApp = app;
		  }
		  public void UpdateLastUsageTime()
		  {
				LastAccessTime = DateTime.UtcNow;
		  }
	 }

	 public class DemoAppRegistry
	 {
		  static readonly TimeSpan FirstAppDisposingInterval = TimeSpan.FromMinutes(21);
		  static readonly TimeSpan GeneralAppDisposingInterval = TimeSpan.FromSeconds(30);
		  static readonly TimeSpan AppDisposeTimeout = TimeSpan.FromMinutes(21);

		  ConcurrentDictionary<string, DemoAppRegistryEntry> AppDictionary { get; set; }
		  bool AppDisposingActivated { get; set; }

		  public DemoAppRegistry()
		  {
				AppDictionary = new ConcurrentDictionary<string, DemoAppRegistryEntry>();
				AppDisposingActivated = false;
		  }

		  public DocumentsDemoApp GetDemoApp(string appId)
		  {
				DemoAppRegistryEntry entry;
				if (!AppDictionary.TryGetValue(appId, out entry))
					 return null;
				entry.UpdateLastUsageTime();
				return entry.DemoApp;
		  }

		  public void RegisterDemoApp(DocumentsDemoApp app)
		  {
				DemoAppRegistryEntry entry = new DemoAppRegistryEntry(app);
				entry.UpdateLastUsageTime();
				AppDictionary.AddOrUpdate(app.Id, entry, (appId, oldEntry) => entry);
				EnsureAppDisposingActivated();
		  }

		  void EnsureAppDisposingActivated()
		  {
				if (!AppDisposingActivated)
				{
					 DisposeOldAppsWithDelay(FirstAppDisposingInterval);
					 AppDisposingActivated = true;
				}
		  }
		  void DisposeOldAppsWithDelay(TimeSpan delay)
		  {
				CommonUtils.ExecuteActionWithDelay(() => OnDelayedOldAppsDisposing(), delay);
		  }
		  void OnDelayedOldAppsDisposing()
		  {
				try
				{
					 DisposeOldApps();
				}
				finally
				{
					 DisposeOldAppsWithDelay(GeneralAppDisposingInterval);
				}
		  }
		  void DisposeOldApps()
		  {
				List<DemoAppRegistryEntry> appEntries = AppDictionary.Values.ToList();
				DateTime timeLowerBoundary = DateTime.UtcNow.Subtract(AppDisposeTimeout);
				foreach (DemoAppRegistryEntry appEntry in appEntries)
				{
					 if (appEntry.LastAccessTime <= timeLowerBoundary)
					 {
						  DemoAppRegistryEntry removedEntry;
						  AppDictionary.TryRemove(appEntry.DemoApp.Id, out removedEntry);
						  appEntry.DemoApp.Dispose();
					 }
				}
		  }
	 }

	 public class SiteModeUtils
	 {
		  const string AppInstanceContextKey = "AppInstanceContextKey_05D8E19B-428C-45BC-B1A0-14C974A994D6";
		  const string AppInstanceSessionKey = "AppInstanceSessionKey_10747C68-A09D-4909-B4FE-C06BF4F8DCDB";
		  static readonly TimeSpan AppInstanceSlidingExpiration = TimeSpan.FromMinutes(20);
		  static HttpContext Context { get { return HttpContext.Current; } }
		  static DemoAppRegistry DemoAppRegistry { get; set; }
		  static readonly object instanceGetterLocker = new object();

		  static SiteModeUtils()
		  {
				DemoAppRegistry = new DemoAppRegistry();
		  }

		  public static DocumentsApp GetAppInstance()
		  {
				if (Context.Items.Contains(AppInstanceContextKey))
					 return Context.Items[AppInstanceContextKey] as DocumentsDemoApp;

				DocumentsDemoApp app = null;
				lock (instanceGetterLocker)
				{
					 string appId = Context.Session[AppInstanceSessionKey] as string;
					 if (appId != null)
						  app = DemoAppRegistry.GetDemoApp(appId);
					 else
					 {
						  appId = Guid.NewGuid().ToString();
						  app = new DocumentsDemoApp(appId);
						  app.Initialize();
						  app.Start();
						  DemoAppRegistry.RegisterDemoApp(app);
						  Context.Session[AppInstanceSessionKey] = appId;
					 }
				}

				Context.Items[AppInstanceContextKey] = app;
				return app;
		  }

		  public static void SafeDeleteDirectory(string dirPath)
		  {
				try
				{
					 if (Directory.Exists(dirPath))
						  Directory.Delete(dirPath, true);
				}
				catch { }
		  }
	 }
}