#region Using Directives
using TRAVERSE.Business;
using TRAVERSE.Business.UserDefault;
using TRAVERSE.Core;
#endregion Using Directives

namespace TRAVERSE.Web.API.Contacts.Models
{
	public static class Utility
    {
		#region Public Methods
		public static string GetDefaultDistCode(string compId)
		{
			EntityList<UserDefaultValue> defaultValues = UserDefault.GetDefaultValues(compId, "AR", "DistCode", UserDefaultContextType.User, ApplicationContext.CurrentUser);
			if (defaultValues != null && defaultValues.Count > 0)
			{
				return defaultValues[0].DefaultValue;
			}
			return null;
		}

		public static string GetDefaultPmtMethodId(string compId)
		{
			EntityList<UserDefaultValue> defaultValues = UserDefault.GetDefaultValues(compId, "AR", "PmtMethodId", UserDefaultContextType.User, ApplicationContext.CurrentUser);
			if (defaultValues != null && defaultValues.Count > 0)
			{
				return defaultValues[0].DefaultValue;
			}
			return null;
		}

		public static string GetDefaultTaxGrpId(string compId)
		{
			EntityList<UserDefaultValue> defaultValues = UserDefault.GetDefaultValues(compId, "AR", "TaxGrpId", UserDefaultContextType.User, ApplicationContext.CurrentUser);
			if (defaultValues != null && defaultValues.Count > 0)
			{
				return defaultValues[0].DefaultValue;
			}
			return null;
		}

		public static string GetDefaultTermsCode(string compId)
		{
			EntityList<UserDefaultValue> defaultValues = UserDefault.GetDefaultValues(compId, "AR", "TermsCode", UserDefaultContextType.User, ApplicationContext.CurrentUser);
			if (defaultValues != null && defaultValues.Count > 0)
			{
				return defaultValues[0].DefaultValue;
			}
			return null;
		}
		#endregion Public Methods
	}
}
