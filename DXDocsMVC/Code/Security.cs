using System.Web.Security;
using System.Linq;
namespace DXDocsMVC.Code
{

	 public class DocumentsMembershipProvider : MembershipProvider
	 {
		  public override bool ValidateUser(string username, string password)
		  {
				User user = DocumentsApp.Instance.User.ValidateUser(username, password);
				return user != null;
		  }

		  #region Methods which are no used in this demo

		  public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion,
																  string passwordAnswer, bool isApproved, object providerUserKey,
																  out MembershipCreateStatus status)
		  {
				throw new System.NotImplementedException();
		  }

		  public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion,
																				 string newPasswordAnswer)
		  {
				throw new System.NotImplementedException();
		  }

		  public override string GetPassword(string username, string answer)
		  {
				throw new System.NotImplementedException();
		  }

		  public override bool ChangePassword(string username, string oldPassword, string newPassword)
		  {
				throw new System.NotImplementedException();
		  }

		  public override string ResetPassword(string username, string answer)
		  {
				throw new System.NotImplementedException();
		  }

		  public override void UpdateUser(MembershipUser user)
		  {
				throw new System.NotImplementedException();
		  }

		  public override bool UnlockUser(string userName)
		  {
				throw new System.NotImplementedException();
		  }

		  public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
		  {
				throw new System.NotImplementedException();
		  }

		  public override MembershipUser GetUser(string username, bool userIsOnline)
		  {
				throw new System.NotImplementedException();
		  }

		  public override string GetUserNameByEmail(string email)
		  {
				throw new System.NotImplementedException();
		  }

		  public override bool DeleteUser(string username, bool deleteAllRelatedData)
		  {
				throw new System.NotImplementedException();
		  }

		  public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
		  {
				throw new System.NotImplementedException();
		  }

		  public override int GetNumberOfUsersOnline()
		  {
				throw new System.NotImplementedException();
		  }

		  public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize,
																					  out int totalRecords)
		  {
				throw new System.NotImplementedException();
		  }

		  public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize,
																						out int totalRecords)
		  {
				throw new System.NotImplementedException();
		  }

		  public override bool EnablePasswordRetrieval
		  {
				get { throw new System.NotImplementedException(); }
		  }

		  public override bool EnablePasswordReset
		  {
				get { throw new System.NotImplementedException(); }
		  }

		  public override bool RequiresQuestionAndAnswer
		  {
				get { throw new System.NotImplementedException(); }
		  }

		  public override string ApplicationName { get; set; }

		  public override int MaxInvalidPasswordAttempts
		  {
				get { throw new System.NotImplementedException(); }
		  }

		  public override int PasswordAttemptWindow
		  {
				get { throw new System.NotImplementedException(); }
		  }

		  public override bool RequiresUniqueEmail
		  {
				get { throw new System.NotImplementedException(); }
		  }

		  public override MembershipPasswordFormat PasswordFormat
		  {
				get { throw new System.NotImplementedException(); }
		  }

		  public override int MinRequiredPasswordLength
		  {
				get { throw new System.NotImplementedException(); }
		  }

		  public override int MinRequiredNonAlphanumericCharacters
		  {
				get { throw new System.NotImplementedException(); }
		  }

		  public override string PasswordStrengthRegularExpression
		  {
				get { throw new System.NotImplementedException(); }
		  }

		  #endregion Methods which are no used in this demo
	 }

	 public class DocumentsRoleProvider : RoleProvider
	 {
		  const string DefaultRole = "User";

		  public override string[] GetRolesForUser(string username)
		  {
				User user = DocumentsApp.Instance.Data.FindUserByAccountName(username);
				return user == null ? new string[] { } : new string[] { DefaultRole };
		  }

		  public override string ApplicationName { get; set; }

		  #region Not used methods

		  public override bool IsUserInRole(string username, string roleName)
		  {
				throw new System.NotImplementedException();
		  }


		  public override void CreateRole(string roleName)
		  {
				throw new System.NotImplementedException();
		  }

		  public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
		  {
				throw new System.NotImplementedException();
		  }

		  public override bool RoleExists(string roleName)
		  {
				throw new System.NotImplementedException();
		  }

		  public override void AddUsersToRoles(string[] usernames, string[] roleNames)
		  {
				throw new System.NotImplementedException();
		  }

		  public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
		  {
				throw new System.NotImplementedException();
		  }

		  public override string[] GetUsersInRole(string roleName)
		  {
				throw new System.NotImplementedException();
		  }

		  public override string[] GetAllRoles()
		  {
				throw new System.NotImplementedException();
		  }

		  public override string[] FindUsersInRole(string roleName, string usernameToMatch)
		  {
				throw new System.NotImplementedException();
		  }

		  #endregion
	 }
}