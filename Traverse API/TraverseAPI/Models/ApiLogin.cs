using System;
using System.ComponentModel.DataAnnotations;
using TRAVERSE.Business.API;

namespace TraverseApi
{
    public class ApiLogin
    {
        [Required(ErrorMessage = "Email address required.", AllowEmptyStrings = false)]
        [DataType(DataType.EmailAddress)]
        public string EmailAddress { get; set; }

        [Required(ErrorMessage = "Password required.", AllowEmptyStrings = false)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        public static bool ValidateUserCred(ApiLogin login, ref string id)
        {
            if (login != null)
            {
                var user = ApiUserProvider.GetUser(TravApiConfig.ApiDatabase, login.EmailAddress, login.Password);
                return !string.IsNullOrEmpty(id = Convert.ToString(user?.Id));
            }

            return false;
        }
    }
}