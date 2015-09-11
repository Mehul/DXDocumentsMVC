using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Runtime.Remoting.Messaging;
using DevExpress.Internal;

namespace DXDocsMVC.Code
{
 
    public abstract class DataService : ServiceBase
	 {
		  public virtual IQueryable<Item> Items { get { throw new NotImplementedException(); } }
		  public virtual IQueryable<User> Users { get { throw new NotImplementedException(); } }
		  public virtual IQueryable<BinaryContent> BinaryContentSet { get { throw new NotImplementedException(); } }

		  public DataService(DocumentsApp app) : base(app) { }

		  public virtual void SaveChanges()
		  {
				throw new NotImplementedException();
		  }
		  public virtual void CloseUnitOfWork()
		  {
				throw new NotImplementedException();
		  }
		  public virtual User FindUserByAccountName(string accountName)
		  {
				throw new NotImplementedException();
		  }
		  public virtual User FindUserById(long id)
		  {
				throw new NotImplementedException();
		  }
		  public virtual User CreateUser()
		  {
				throw new NotImplementedException();
		  }
		  public virtual void DeleteUser(long id)
		  {
				throw new NotImplementedException();
		  }

		  public virtual Item FindItemById(long id)
		  {
				throw new NotImplementedException();
		  }
		  public virtual void DeleteItem(Item item)
		  {
				throw new NotImplementedException();
		  }
		  public virtual void AddItem(Item item)
		  {
				throw new NotImplementedException();
		  }

		  public virtual BinaryContent CreateBinaryContent(byte[] data)
		  {
				throw new NotImplementedException();
		  }
		  public virtual BinaryContent CreateBinaryContent(Stream dataStream)
		  {
				throw new NotImplementedException();
		  }

		  public virtual Stream ReadFileContent(Item fileItem)
		  {
				throw new NotImplementedException();
		  }
		  public virtual int GetFileSize(Item fileItem)
		  {
				throw new NotImplementedException();
		  }

		  public virtual void Initialize()
		  {
				throw new NotImplementedException();
		  }
	 }

	 public class EntityDataService : DXDocsMVC.Code.DataService
	 {
		  const string DataContextKey = "11F33BBC-0EBC-47DE-857E-9EF6DB452DAC";
		  const string ConnectionStringName = "DocumentsConnectionString";
		  static HttpContext Context { get { return HttpContext.Current; } }
		  string ConnectionString
		  {
				get
				{
					 string connectionString = System.Web.Configuration.WebConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
					 return DbEngineDetector.PatchConnectionString(connectionString);
				}
		  }
		  DXDocsMVC.Code.DocumentsDb DataContext
		  {
				get
				{
					 return IsInRequestScope() ? GetDataContextInRequestScope() : GetDataContextInCallContextScope();
				}
		  }

		  bool IsInRequestScope()
		  {
				return Context != null;
		  }

		  public EntityDataService(DocumentsApp app) : base(app) { }

		  DXDocsMVC.Code.DocumentsDb GetDataContextInRequestScope()
		  {
				if (!Context.Items.Contains(DataContextKey))
					 Context.Items[DataContextKey] = CreateDataContext();
				return (DXDocsMVC.Code.DocumentsDb)Context.Items[DataContextKey];
		  }
		  void DisposeDataContextInRequestScope()
		  {
				if (Context.Items.Contains(DataContextKey))
				{
					 DXDocsMVC.Code.DocumentsDb dataContext = (DXDocsMVC.Code.DocumentsDb)Context.Items[DataContextKey];
					 dataContext.Dispose();
					 Context.Items.Remove(DataContextKey);
				}
		  }
		  DXDocsMVC.Code.DocumentsDb GetDataContextInCallContextScope()
		  {
				DXDocsMVC.Code.DocumentsDb dataContext = CallContext.GetData(DataContextKey) as DXDocsMVC.Code.DocumentsDb;
				if (dataContext == null)
				{
					 dataContext = CreateDataContext();
					 CallContext.SetData(DataContextKey, dataContext);
				}
				return dataContext;
		  }
		  void DisposeDataContextInCallContextScope()
		  {
				DXDocsMVC.Code.DocumentsDb dataContext = CallContext.GetData(DataContextKey) as DXDocsMVC.Code.DocumentsDb;
				if (dataContext != null)
				{
					 dataContext.Dispose();
					 CallContext.SetData(DataContextKey, null);
				}
		  }
		  DXDocsMVC.Code.DocumentsDb CreateDataContext()
		  {
				return new DXDocsMVC.Code.DocumentsDb(ConnectionString);
		  }

		  public override IQueryable<Item> Items { get { return DataContext.Items; } }
		  public override IQueryable<User> Users { get { return DataContext.Users; } }
		  public override IQueryable<BinaryContent> BinaryContentSet { get { return DataContext.BinaryContentSet; } }

		  public override void SaveChanges()
		  {
				DataContext.SaveChanges();
		  }
		  public override void CloseUnitOfWork()
		  {
				if (IsInRequestScope())
					 DisposeDataContextInRequestScope();
				else
					 DisposeDataContextInCallContextScope();
		  }

		  public override User FindUserByAccountName(string accountName)
		  {
				return Users.FirstOrDefault(user => user.AccountName == accountName);
		  }
		  public override User FindUserById(long id)
		  {
				return DataContext.Users.Find(id);
		  }
		  public override User CreateUser()
		  {
				User user = DataContext.Users.Create();
				return DataContext.Users.Add(user);
		  }
		  public override void DeleteUser(long id)
		  {
				User user = FindUserById(id);
				if (user == null)
					 return;
				DataContext.Users.Remove(user);
				DataContext.SaveChanges();
		  }

		  public override Item FindItemById(long id)
		  {
				return DataContext.Items.Find(id);
		  }
		  public override void DeleteItem(Item item)
		  {
				if (item.IsFolder)
				{
					 foreach (Item childItem in item.ChildItems.ToList())
						  DeleteItem(childItem);
				}
				else
					 DataContext.BinaryContentSet.Remove(item.Content);
				DataContext.Items.Remove(item);
		  }
		  public override void AddItem(Item item)
		  {
				if (item.Owner != null)
					 item.Owner = FindUserById(item.Owner.Id);
				DataContext.Items.Add(item);
		  }

		  public override BinaryContent CreateBinaryContent(byte[] data)
		  {
				BinaryContent content = DataContext.BinaryContentSet.Create();
				content.Data = data;
				return DataContext.BinaryContentSet.Add(content);
		  }
		  public override BinaryContent CreateBinaryContent(Stream dataStream)
		  {
				byte[] data = CommonUtils.ReadAllBytes(dataStream);
				return CreateBinaryContent(data);
		  }

		  public override Stream ReadFileContent(Item fileItem)
		  {
				return new MemoryStream(fileItem.Content.Data.ToArray());
		  }
		  public override int GetFileSize(Item fileItem)
		  {
				int? size = Items.
					 Where(item => item.Id == fileItem.Id).
					 Join(BinaryContentSet, item => item.Content.Id, content => content.Id, (item, content) => content).
					 Select(content => SqlFunctions.DataLength(content.Data)).
					 FirstOrDefault();

				return size.GetValueOrDefault();
		  }

		  public override void Initialize()
		  {
				//Database.SetInitializer(new DemoDataInitializer());
				Database.SetInitializer<DXDocsMVC.Code.DocumentsDb>(null);
		  }
	 }

	 public class DocumentsDb : DbContext
	 {
		  public DocumentsDb() : base() { }
		  public DocumentsDb(string connectionString) : base(connectionString) { }

		  public DbSet<Item> Items { get; set; }
		  public DbSet<User> Users { get; set; }
		  public DbSet<BinaryContent> BinaryContentSet { get; set; }

		  protected override void OnModelCreating(DbModelBuilder modelBuilder)
		  {
				var itemConfig = modelBuilder.Entity<Item>();
				itemConfig.HasKey(i => i.Id);
				itemConfig.Property(i => i.Name).IsRequired();
				itemConfig.Property(i => i.IsFolder).IsRequired();
				itemConfig.HasOptional(i => i.Content);
				itemConfig.HasOptional(i => i.Owner);
				itemConfig.HasOptional(i => i.ParentItem)
					 .WithMany(pi => pi.ChildItems);

				var userConfig = modelBuilder.Entity<User>();
				userConfig.HasKey(u => u.Id);
				userConfig.Property(u => u.Name).IsRequired();
				userConfig.Property(u => u.AccountName).IsRequired();
				userConfig.HasOptional(u => u.Avatar);

				var binaryContentConfig = modelBuilder.Entity<BinaryContent>();
				binaryContentConfig.HasKey(bc => bc.Id);

				DemoUtils.IgnoreDemoClasses(modelBuilder);

				base.OnModelCreating(modelBuilder);
		  }
	 }

	 public class DemoDataInitializer : CreateDatabaseIfNotExists<DXDocsMVC.Code.DocumentsDb>
	 {
		  const string FilesVirtPath = @"~\App_Data\SampleData\Files";
		  const string UsersVirtPath = @"~\App_Data\SampleData\Users";

		  static string FilesPath
		  {
				get { return CommonUtils.MapPath(FilesVirtPath); }
		  }
		  static string UsersPath
		  {
				get { return CommonUtils.MapPath(UsersVirtPath); }
		  }

		  protected override void Seed(DXDocsMVC.Code.DocumentsDb context)
		  {
				base.Seed(context);

				User[] users = CreateUsers(context);
				LoadDirectoryToDb(FilesPath, context, null, users, 0);

				context.SaveChanges();
		  }
		  User[] CreateUsers(DXDocsMVC.Code.DocumentsDb context)
		  {
				string benAvatarPath = Path.Combine(UsersPath, "Ben Stevenson.jpg");
				string maryAvatarPath = Path.Combine(UsersPath, "Maria Hernandez.jpg");
				string gabeAvatarPath = Path.Combine(UsersPath, "Gabriel Santiago.jpg");

				User[] users = new User[] {
            new User { AccountName = "Ben", Name = "Ben Stevenson", Avatar = CreateBinaryContent(context, benAvatarPath) },
            new User { AccountName = "Mary", Name = "Maria Hernandez", Avatar = CreateBinaryContent(context, maryAvatarPath) },
            new User { AccountName = "Gabe", Name = "Gabriel Santiago", Avatar = CreateBinaryContent(context, gabeAvatarPath) }
        };
				foreach (User user in users)
					 context.Users.Add(user);

				return users;
		  }
		  BinaryContent CreateBinaryContent(DXDocsMVC.Code.DocumentsDb context, string filePath)
		  {
				BinaryContent content = new BinaryContent();
				content.Data = File.ReadAllBytes(filePath);
				context.BinaryContentSet.Add(content);
				return content;
		  }
		  void LoadDirectoryToDb(string dirPath, DXDocsMVC.Code.DocumentsDb context, Item parentItem, User[] users, int itemIndex)
		  {
				DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
				Item dirItem = CreateItemFromFileSystemInfo(dirInfo, true, parentItem);
				if (itemIndex != 0)
					 dirItem.Owner = users[itemIndex % users.Length];
				context.Items.Add(dirItem);
				itemIndex++;
				foreach (FileInfo fileInfo in dirInfo.EnumerateFiles())
				{
					 Item fileItem = CreateItemFromFileSystemInfo(fileInfo, false, dirItem);
					 fileItem.Owner = users[itemIndex % users.Length];
					 fileItem.Content = CreateBinaryContent(context, fileInfo.FullName);
					 context.Items.Add(fileItem);
					 itemIndex++;
				}
				foreach (DirectoryInfo subDirInfo in dirInfo.EnumerateDirectories())
				{
					 LoadDirectoryToDb(subDirInfo.FullName, context, dirItem, users, itemIndex);
				}

		  }
		  Item CreateItemFromFileSystemInfo(FileSystemInfo fsi, bool isFolder, Item parentItem)
		  {
				return new Item()
				{
					 ParentItem = parentItem,
					 Name = fsi.Name,
					 IsFolder = isFolder,
					 CreationTime = fsi.CreationTimeUtc,
					 LastAccessTime = fsi.LastAccessTimeUtc,
					 LastWriteTime = fsi.LastWriteTimeUtc
				};
		  }
	 }
}