using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.Caching;
using System.Web.UI;
using DevExpress.Web;
using DevExpress.Web.Internal;
namespace DXDocsMVC.Code
{

	 public class CommonUtils
	 {

		  const string DelayedActionKeyPrefix = "AppTask_";
		  static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB" };

		  public static void ExecuteActionWithDelay(Action action, TimeSpan delay)
		  {
				string cacheKey = DelayedActionKeyPrefix + Guid.NewGuid().ToString();
				CacheItemRemovedCallback callback = (key, value, reason) =>
				{
					 if (reason == CacheItemRemovedReason.Expired)
						  action();
				};
				HttpRuntime.Cache.Insert(cacheKey, string.Empty, null, DateTime.UtcNow + delay,
					 Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, callback);
		  }

		  public static byte[] ReadAllBytes(Stream stream)
		  {
				byte[] buffer = new byte[16 * 1024];
				int readCount;
				using (MemoryStream ms = new MemoryStream())
				{
					 while ((readCount = stream.Read(buffer, 0, buffer.Length)) > 0)
					 {
						  ms.Write(buffer, 0, readCount);
					 }
					 return ms.ToArray();
				}
		  }

		  public static string MapPath(string virtPath)
		  {
				if (HttpContext.Current != null)
					 return HttpContext.Current.Server.MapPath(virtPath);
				return UrlUtils.ResolvePhysicalPath(virtPath);
		  }

		  public static string ConvertVirtPathToUrl(string virtPath)
		  {
				return virtPath.Replace("~\\", "").Replace('\\', '/');
		  }

		  public static TControl FindControl<TControl>(Control container, string id) where TControl : Control
		  {
				Control result = container.FindControl(id);
				if (result != null)
					 return (TControl)result;
				foreach (Control childControl in container.Controls)
				{
					 result = FindControl<TControl>(childControl, id);
					 if (result != null)
						  return (TControl)result;
				}
				return null;
		  }

		  public static string GetSizeString(long value)
		  {
				if (value == 0)
					 return "0 B";

				int maxRank = SizeSuffixes.Length - 1;
				int rank = (int)Math.Log(value, 1024);
				if (rank > maxRank)
					 rank = maxRank;
				decimal adjustedSize = (decimal)value / (1L << (rank * 10));

				return string.Format("{0:n0} {1}", adjustedSize, SizeSuffixes[rank]);
		  }
	 }
}