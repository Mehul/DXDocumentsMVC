using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using System.Web.Security;
using DevExpress.Web;
namespace DXDocsMVC.Code
{

	 public class UserService : ServiceBase
	 {
		  const string CurrentUserSessionKey = "CurrentUser_246CFE91-544D-49A9-BA9D-BCDC4503B4BA";
		  const string CurrentViewModeCookieKey = "CurrentViewMode";
		  public const string DefaultUserAccountName = "Ben";

		  static HttpContext Context { get { return HttpContext.Current; } }
		  static readonly TimeSpan ActivityWatchingInterval = TimeSpan.FromMinutes(1);

		  DXDocsMVC.Code.DataService DataService { get { return DocumentsApp.Data; } }
		  DocumentService DocumentService { get { return DocumentsApp.Document; } }

		  ConcurrentDictionary<string, DateTime> ActivityRegistry { get; set; }

		  public User CurrentUser
		  {
				get
				{
					 string accountName = GetCurrentUserAccountName();
					 if (accountName == null)
					 {
						  Context.Session[CurrentUserSessionKey] = null;
						  return null;
					 }

					 User user = Context.Session[CurrentUserSessionKey] as User;
					 if (user == null || user.AccountName != accountName)
					 {
						  user = GetCurrentUser();
						  Context.Session[CurrentUserSessionKey] = user;
					 }

					 return user;
				}
				private set
				{
					 Context.Session[CurrentUserSessionKey] = value;
				}
		  }
		  public string CurrentViewModeName
		  {
				get
				{
					 HttpCookie cookie = Context.Request.Cookies[CurrentViewModeCookieKey];
					 if (cookie == null)
						  return null;
					 return HttpUtility.UrlDecode(cookie.Value);
				}
		  }

		  public UserService(DocumentsApp app)
				: base(app)
		  {
				ActivityRegistry = new ConcurrentDictionary<string, DateTime>();
		  }

		  public bool SignIn(string userAccountName, string userPassword)
		  {
				if (string.IsNullOrEmpty(userPassword))
					 userPassword = null;
				if (Membership.ValidateUser(userAccountName, userPassword))
				{
					 FormsAuthentication.SetAuthCookie(userAccountName, false);
					 FormsAuthentication.RedirectFromLoginPage(userAccountName, false);
					 return true;
				}
				return false;
		  }
		  public void SignOut()
		  {
				DocumentService.RemoveAllLocksForUser(CurrentUser);
				FormsAuthentication.SignOut();
				CurrentUser = null;
				FormsAuthentication.RedirectToLoginPage();
		  }
		  public User ValidateUser(string accountName, string password)
		  {
				return DataService.Users.FirstOrDefault(user => user.AccountName == accountName && user.Password == password);
		  }
		  public void LogUserActivity()
		  {
				string accountName = GetCurrentUserAccountName();
				if (accountName != null)
					 ActivityRegistry.AddOrUpdate(accountName, DateTime.Now, (name, oldDate) => DateTime.Now);
		  }
		  public void CheckActivityWithDelay()
		  {
				CommonUtils.ExecuteActionWithDelay(() => OnDelayedActivityChecking(), ActivityWatchingInterval);
		  }

		  User GetCurrentUser()
		  {
				string accountName = GetCurrentUserAccountName();
				if (accountName == null)
					 return null;
				return DataService.FindUserByAccountName(accountName);
		  }
		  string GetCurrentUserAccountName()
		  {
				if (Context.User != null && Context.User.Identity.IsAuthenticated)
					 return Context.User.Identity.Name;
				return null;
		  }
		  void OnDelayedActivityChecking()
		  {
				try
				{
					 CheckActivity();
				}
				finally
				{
					 CheckActivityWithDelay();
					 DataService.CloseUnitOfWork();
				}
		  }
		  void CheckActivity()
		  {
				var userInfoList = ActivityRegistry.
					 Select(entry => new { AccountName = entry.Key, LastActivityDate = entry.Value }).
					 ToList();

				DateTime timeLowerBoundary = DateTime.UtcNow.Subtract(FormsAuthentication.Timeout);
				foreach (var userInfo in userInfoList)
				{
					 DateTime activityDate;
					 if (userInfo.LastActivityDate.ToUniversalTime() < timeLowerBoundary)
					 {
						  ActivityRegistry.TryRemove(userInfo.AccountName, out activityDate);
						  User user = DataService.FindUserByAccountName(userInfo.AccountName);
						  DocumentService.RemoveAllLocksForUser(user);
					 }
				}
		  }
	 }
}