using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using DevExpress.Web;
namespace DXDocsMVC.Code
{

	 public class FileSystemService : ServiceBase
	 {
		  const int MaxRecentFilesCount = 20;
		  const long MaxFileSizeForUpload = 10485760;

		  DXDocsMVC.Code.DataService DataService { get { return DocumentsApp.Data; } }
		  UserService UserService { get { return DocumentsApp.User; } }

		  public FileSystemService(DocumentsApp app) : base(app) { }

		  public Item GetFileFromSource(string filePath, string dataSource)
		  {
				if (dataSource == "All")
					 return FindItem(filePath);
				var query = GetFilesFromSource(dataSource);
				if (query == null)
					 return null;
				string fileName = Path.GetFileName(filePath);
				return query.FirstOrDefault(file => file.Name == fileName);
		  }
		  public IEnumerable<Item> GetFilesFromSource(string dataSource)
		  {
				switch (dataSource)
				{
					 case "My":
						  return GetMyFiles();
					 case "Recent":
						  return GetRecentFiles();
					 case "RTFDocs":
						  return GetRTFFiles();
					 case "Sheets":
						  return GetSheetFiles();
				}
				return null;
		  }
		  public virtual Item CreateNewFileItem(string folderPath, string fileName)
		  {
				Item folderItem = FindItem(folderPath);
				string baseFileName = Path.GetFileNameWithoutExtension(fileName);
				string fileExtension = Path.GetExtension(fileName);
				string uniqueFileName = fileName;
				string newFilePath = Path.Combine(folderPath, uniqueFileName);
				int counter = 1;
				while (FindItem(newFilePath) != null)
				{
					 uniqueFileName = baseFileName + " (" + counter + ")" + fileExtension;
					 newFilePath = Path.Combine(folderPath, uniqueFileName);
					 counter++;
				}
				Item newFileItem = new Item()
				{
					 Name = uniqueFileName,
					 Content = DataService.CreateBinaryContent(new byte[0]),
					 Owner = UserService.CurrentUser,
					 ParentItem = folderItem,
					 CreationTime = DateTime.UtcNow,
					 LastAccessTime = DateTime.UtcNow,
					 LastWriteTime = DateTime.UtcNow,
					 IsFolder = false
				};
				DataService.AddItem(newFileItem);
				DataService.SaveChanges();
				return newFileItem;
		  }
		  public Item FindItem(string relativeName)
		  {
				string[] itemNames = relativeName.Split(Path.DirectorySeparatorChar);
				string rootItemName = itemNames[0];
				var query = DataService.Items.
					 Where(item => item.ParentItem == null && item.IsFolder && item.Name == rootItemName);

				for (int i = 1; i < itemNames.Length; i++)
				{
					 string itemName = itemNames[i];
					 query = DataService.Items.Join(query,
								childItem => childItem.ParentItem == null ? (long?)null : childItem.ParentItem.Id,
								parentItem => parentItem.Id,
								(childItem, parentItem) => childItem).
						  Where(item => item.Name == itemName);
				}

				return query.FirstOrDefault();
		  }
		  public DateTime GetFolderLastWriteTime(Item folderItem)
		  {
				DateTime result = DataService.Items.
					 Where(item => item.ParentItem != null && item.ParentItem.Id == folderItem.Id && !item.IsFolder).
					 Select(item => item.LastWriteTime).
					 OrderByDescending(time => time).
					 FirstOrDefault();

				if (result == default(DateTime) || result < folderItem.LastWriteTime)
					 result = folderItem.LastWriteTime;

				return result;
		  }
		  public virtual long GetMaxFileSizeForUpload()
		  {
				return MaxFileSizeForUpload;
		  }

		  IEnumerable<Item> GetAllFiles()
		  {
				return DataService.Items.Where(item => !item.IsFolder);
		  }
		  IEnumerable<Item> GetFilesByExtensions(string[] extensions)
		  {
				return GetAllFiles().
					 Where(item => extensions.Contains(Path.GetExtension(item.Name))).
					 OrderByDescending(item => item.LastWriteTime);
		  }

		  IEnumerable<Item> GetMyFiles()
		  {
				User currentUser = UserService.CurrentUser;
				return GetAllFiles().
					 Where(item => item.Owner.Id == currentUser.Id).
					 OrderByDescending(item => item.LastWriteTime).
					 Take(MaxRecentFilesCount).
					 GroupBy(item => item.Name).
					 Select(itemGroup => itemGroup.First());
		  }
		  IEnumerable<Item> GetRecentFiles()
		  {
				return GetAllFiles().
					 OrderByDescending(item => item.LastWriteTime).
					 Take(MaxRecentFilesCount).
					 GroupBy(item => item.Name).
					 Select(itemGroup => itemGroup.First());
		  }
		  IEnumerable<Item> GetRTFFiles()
		  {
				string[] extensions = new[] { ".doc", ".docx", ".rtf" };
				return GetFilesByExtensions(extensions);
		  }
		  IEnumerable<Item> GetSheetFiles()
		  {
				string[] extensions = new[] { ".xlsx" };
				return GetFilesByExtensions(extensions);
		  }
	 }

	 public class FilteredFileSystemProvider : FileSystemProviderBase
	 {
		  DocumentsApp DocumentsApp { get; set; }
		  DXDocsMVC.Code.DataService Data { get { return DocumentsApp.Data; } }

		  Dictionary<long, long> FileSizeCache { get; set; }

		  public FilteredFileSystemProvider(DocumentsApp app, string rootFolder)
				: base(rootFolder)
		  {
				DocumentsApp = app;
				FileSizeCache = new Dictionary<long, long>();
		  }

		  IEnumerable<Item> fileQuery;
		  public IEnumerable<Item> FileQuery
		  {
				get
				{
					 return this.fileQuery;
				}
				set
				{
					 this.fileQuery = value;
					 this.fileQueryInternal = null;
				}
		  }
		  IEnumerable<Item> fileQueryInternal;
		  IEnumerable<Item> FileQueryInternal
		  {
				get
				{
					 if (this.fileQueryInternal == null)
						  this.fileQueryInternal = FileQuery.ToList();
					 return this.fileQueryInternal;
				}
		  }

		  Item FindFileItem(FileManagerFile file)
		  {
				return FileQueryInternal.FirstOrDefault(item => item.Id.ToString() == file.Id);
		  }
		  bool IsRootFolder(FileManagerFolder folder)
		  {
				return string.IsNullOrEmpty(folder.RelativeName);
		  }

		  public override string RootFolderDisplayName { get { return ""; } }

		  public override IEnumerable<FileManagerFile> GetFiles(FileManagerFolder folder)
		  {
				if (!IsRootFolder(folder))
					 return new FileManagerFile[0];
				return FileQueryInternal.Select(item => new FileManagerFile(this, folder, item.Name, item.Id.ToString()));
		  }
		  public override IEnumerable<FileManagerFolder> GetFolders(FileManagerFolder parentFolder)
		  {
				return new FileManagerFolder[0];
		  }
		  public override bool Exists(FileManagerFile file)
		  {
				return FindFileItem(file) != null;
		  }
		  public override bool Exists(FileManagerFolder folder)
		  {
				return IsRootFolder(folder);
		  }
		  public override Stream ReadFile(FileManagerFile file)
		  {
				Item fileItem = FindFileItem(file);
				return Data.ReadFileContent(fileItem);
		  }
		  public override DateTime GetLastWriteTime(FileManagerFile file)
		  {
				Item fileItem = FindFileItem(file);
				return fileItem.LastWriteTime;
		  }

		  public override string GetThumbnailUrl(FileManagerFile file)
		  {
				Item fileItem = FindFileItem(file);
				return DocumentsApp.Image.GetThumbnailUrl(fileItem);
		  }

		  public override void CreateFolder(FileManagerFolder parent, string name)
		  {
		  }
		  public override void DeleteFile(FileManagerFile file)
		  {
				Item fileItem = FindFileItem(file);
				Data.DeleteItem(fileItem);
				Data.SaveChanges();
		  }
		  public override void DeleteFolder(FileManagerFolder folder)
		  {
		  }
		  public override void MoveFile(FileManagerFile file, FileManagerFolder newParentFolder)
		  {
		  }
		  public override void MoveFolder(FileManagerFolder folder, FileManagerFolder newParentFolder)
		  {
		  }
		  public override void RenameFile(FileManagerFile file, string name)
		  {
		  }
		  public override void RenameFolder(FileManagerFolder folder, string name)
		  {
		  }
		  public override void UploadFile(FileManagerFolder folder, string fileName, Stream content)
		  {
		  }
		  public override void CopyFile(FileManagerFile file, FileManagerFolder newParentFolder)
		  {
		  }
		  public override void CopyFolder(FileManagerFolder folder, FileManagerFolder newParentFolder)
		  {
		  }
		  public override long GetLength(FileManagerFile file)
		  {
				long result;
				Item fileItem = FindFileItem(file);
				if (!FileSizeCache.TryGetValue(fileItem.Id, out result))
				{
					 result = Data.GetFileSize(fileItem);
					 FileSizeCache.Add(fileItem.Id, result);
				}
				return result;
		  }
	 }

	 public class GeneralFileSystemProvider : FileSystemProviderBase
	 {
		  DocumentsApp DocumentsApp { get; set; }
		  DXDocsMVC.Code.DataService Data { get { return DocumentsApp.Data; } }

		  Dictionary<long, Item> FolderCache { get; set; }
		  Dictionary<long, DateTime> FolderLastWriteTimeCache { get; set; }
		  Dictionary<long, long> FileSizeCache { get; set; }
		  Item RootItem { get; set; }

		  public GeneralFileSystemProvider(DocumentsApp app, string rootFolder)
				: base(rootFolder)
		  {
				DocumentsApp = app;
				FolderLastWriteTimeCache = new Dictionary<long, DateTime>();
				FileSizeCache = new Dictionary<long, long>();
				RefreshFolderCache();
		  }

		  public override string RootFolderDisplayName { get { return RootItem.Name; } }

		  public override IEnumerable<FileManagerFile> GetFiles(FileManagerFolder folder)
		  {
				long folderId = GetItemId(folder);
				return Data.Items.
					 Where(item => item.ParentItem != null && item.ParentItem.Id == folderId && !item.IsFolder).
					 ToList().
					 Select(item => new FileManagerFile(this, folder, item.Name, item.Id.ToString()));
		  }
		  public override IEnumerable<FileManagerFolder> GetFolders(FileManagerFolder parentFolder)
		  {
				Item folderItem = FindFolderItem(parentFolder);
				return FolderCache.Values.
					 Where(item => item.ParentItem == folderItem && item.IsFolder).
					 Select(item => new FileManagerFolder(this, parentFolder, item.Name, item.Id.ToString()));
		  }
		  public override bool Exists(FileManagerFile file)
		  {
				return FindFileItem(file) != null;
		  }
		  public override bool Exists(FileManagerFolder folder)
		  {
				return FindFolderItem(folder) != null;
		  }
		  public override Stream ReadFile(FileManagerFile file)
		  {
				Item fileItem = FindFileItem(file);
				return Data.ReadFileContent(fileItem);
		  }
		  public override DateTime GetLastWriteTime(FileManagerFile file)
		  {
				Item fileItem = FindFileItem(file);
				return fileItem.LastWriteTime;
		  }
		  public override DateTime GetLastWriteTime(FileManagerFolder folder)
		  {
				DateTime result;
				Item folderItem = FindFolderItem(folder);
				if (!FolderLastWriteTimeCache.TryGetValue(folderItem.Id, out result))
				{
					 result = DocumentsApp.FileSystem.GetFolderLastWriteTime(folderItem);
					 FolderLastWriteTimeCache.Add(folderItem.Id, result);
				}
				return result;
		  }

		  public override string GetThumbnailUrl(FileManagerFile file)
		  {
				Item fileItem = FindFileItem(file);
				return DocumentsApp.Image.GetThumbnailUrl(fileItem);
		  }

		  public override void CreateFolder(FileManagerFolder parent, string name)
		  {
				UpdateAndSaveChanges(parent,
					 parentItem => Data.AddItem(new Item()
					 {
						  IsFolder = true,
						  Owner = DocumentsApp.User.CurrentUser,
						  CreationTime = DateTime.UtcNow,
						  LastAccessTime = DateTime.UtcNow,
						  LastWriteTime = DateTime.UtcNow,
						  Name = name,
						  ParentItem = parentItem
					 }));
		  }
		  public override void DeleteFile(FileManagerFile file)
		  {
				UpdateAndSubmitChanges(file, item => Data.DeleteItem(item));
		  }
		  public override void DeleteFolder(FileManagerFolder folder)
		  {
				UpdateAndSaveChanges(folder, item => Data.DeleteItem(item));
		  }
		  public override void MoveFile(FileManagerFile file, FileManagerFolder newParentFolder)
		  {
				UpdateAndSubmitChanges(file, item => item.ParentItem = FindFolderItem(newParentFolder));
		  }
		  public override void MoveFolder(FileManagerFolder folder, FileManagerFolder newParentFolder)
		  {
				UpdateAndSaveChanges(folder, item => item.ParentItem = FindFolderItem(newParentFolder));
		  }
		  public override void RenameFile(FileManagerFile file, string name)
		  {
				UpdateAndSubmitChanges(file, item => item.Name = name);
		  }
		  public override void RenameFolder(FileManagerFolder folder, string name)
		  {
				UpdateAndSaveChanges(folder, item => item.Name = name);
		  }
		  public override void UploadFile(FileManagerFolder folder, string fileName, Stream content)
		  {
				UpdateAndSaveChanges(folder,
					 folderItem => Data.AddItem(new Item()
					 {
						  Name = fileName,
						  Content = Data.CreateBinaryContent(content),
						  Owner = DocumentsApp.User.CurrentUser,
						  ParentItem = folderItem,
						  CreationTime = DateTime.UtcNow,
						  LastAccessTime = DateTime.UtcNow,
						  LastWriteTime = DateTime.UtcNow,
						  IsFolder = false
					 }));
		  }
		  public override void CopyFile(FileManagerFile file, FileManagerFolder newParentFolder)
		  {
				Item fileItem = FindFileItem(file);
				CopyCore(fileItem, newParentFolder.RelativeName, false);
		  }
		  public override void CopyFolder(FileManagerFolder folder, FileManagerFolder newParentFolder)
		  {
				List<FileManagerFolder> folders = new List<FileManagerFolder>();
				FillSubFoldersList(folder, folders);
				int folderNameOffset = !string.IsNullOrEmpty(folder.Parent.RelativeName) ? folder.Parent.RelativeName.Length + 1 : 0;

				foreach (FileManagerFolder copyingFolder in folders)
				{
					 Item folderItem = FindFolderItem(copyingFolder);
					 string folderPath = newParentFolder.RelativeName;
					 if (copyingFolder != folder)
						  folderPath = Path.Combine(folderPath, copyingFolder.Parent.RelativeName.Substring(folderNameOffset));
					 CopyCore(folderItem, folderPath, true);
					 foreach (FileManagerFile currentFile in copyingFolder.GetFiles())
					 {
						  Item fileItem = FindFileItem(currentFile);
						  CopyCore(fileItem, Path.Combine(folderPath, copyingFolder.Name), false);
					 }
				}
		  }
		  void FillSubFoldersList(FileManagerFolder folder, List<FileManagerFolder> list)
		  {
				list.Add(folder);
				foreach (FileManagerFolder subFolder in folder.GetFolders())
					 FillSubFoldersList(subFolder, list);
		  }
		  void CopyCore(Item item, string path, bool isFolder)
		  {
				FileManagerFolder newParentFolder = new FileManagerFolder(this, path, (string)null);
				UpdateAndSaveChanges(newParentFolder,
					 newParentItem => Data.AddItem(new Item()
					 {
						  Name = item.Name,
						  Content = item.IsFolder ? null : Data.CreateBinaryContent(item.Content.Data),
						  ParentItem = newParentItem,
						  Owner = DocumentsApp.User.CurrentUser,
						  CreationTime = DateTime.UtcNow,
						  LastAccessTime = DateTime.UtcNow,
						  LastWriteTime = DateTime.UtcNow,
						  IsFolder = isFolder
					 }));
		  }
		  protected void UpdateAndSubmitChanges(FileManagerFile file, Action<Item> update)
		  {
				UpdateAndSubmitChangesCore(FindFileItem(file), update);
		  }
		  protected void UpdateAndSaveChanges(FileManagerFolder folder, Action<Item> update)
		  {
				UpdateAndSubmitChangesCore(FindFolderItem(folder), update);
		  }
		  protected void UpdateAndSubmitChangesCore(Item item, Action<Item> update)
		  {
				update(item);
				Data.SaveChanges();
				RefreshFolderCache();
				ClearFolderLastWriteTimeCache();
				if (!item.IsFolder && FileSizeCache.ContainsKey(item.Id))
					 FileSizeCache.Remove(item.Id);
		  }
		  protected Item FindFileItem(FileManagerFile file)
		  {
				if (file.Id == null)
					 return FindFileItemByParentFolder(file.Name, file.Folder);

				long fileId = GetItemId(file);
				return Data.FindItemById(fileId);
		  }
		  protected Item FindFolderItem(FileManagerFolder folder)
		  {
				if (folder.Id == null)
					 return FindFolderItemByRelativeName(folder);

				long folderId = GetItemId(folder);
				return FolderCache[folderId];
		  }
		  protected Item FindFileItemByParentFolder(string itemName, FileManagerFolder parentFolder)
		  {
				Item parentItem = FindFolderItemByRelativeName(parentFolder);
				return Data.Items.FirstOrDefault(
					 item => item.ParentItem != null && item.ParentItem.Id == parentItem.Id && !item.IsFolder && item.Name == itemName);
		  }
		  protected long GetItemId(FileManagerItem fileManagerItem)
		  {
				if (string.IsNullOrEmpty(fileManagerItem.RelativeName))
					 return RootItem.Id;
				return long.Parse(fileManagerItem.Id);
		  }
		  protected Item FindFolderItemByRelativeName(FileManagerFolder folder)
		  {
				return FolderCache.Values.
					 Where(item => item.IsFolder && GetRelativeName(item) == folder.RelativeName).
					 FirstOrDefault();
		  }
		  protected string GetRelativeName(Item folderItem)
		  {
				if (folderItem.IsRoot)
					 return string.Empty;
				if (folderItem.ParentItem.IsRoot)
					 return folderItem.Name;
				string name = GetRelativeName(folderItem.ParentItem);
				return Path.Combine(name, folderItem.Name);
		  }

		  protected void RefreshFolderCache()
		  {
				FolderCache = Data.Items.
					 Where(item => item.IsFolder).
					 ToDictionary(item => item.Id);

				RootItem = FolderCache.Values.First(item => item.IsRoot);
		  }
		  protected void ClearFolderLastWriteTimeCache()
		  {
				FolderLastWriteTimeCache.Clear();
		  }

		  public override long GetLength(FileManagerFile file)
		  {
				long result;
				Item fileItem = FindFileItem(file);
				if (!FileSizeCache.TryGetValue(fileItem.Id, out result))
				{
					 result = Data.GetFileSize(fileItem);
					 FileSizeCache.Add(fileItem.Id, result);
				}
				return result;
		  }
	 }
}