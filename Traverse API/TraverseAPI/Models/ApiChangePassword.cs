using OSI.TraverseApi.Business;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace TraverseApi
{
    public class ApiChangePassword
    {
        [Required(ErrorMessage = "Old password is required.", AllowEmptyStrings = false)]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Password is required.", AllowEmptyStrings = false)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required.", AllowEmptyStrings = false)]
        [DataType(DataType.Password), CustomValidation(typeof(ApiChangePassword), "ValidatePassword", ErrorMessage = "Password and confirm password must match")]
        public string ConfirmPassword { get; set; }

        public static ValidationResult ValidatePassword(object value, ValidationContext context)
        {
            if ((value as string) == null)
                return new ValidationResult("No password provided");
            else
            {
                string valueMember = context.MemberName == "Password" ? "ConfirmPassword" : "Password";
                var property = context.ObjectType.GetProperty(valueMember);
                if (property == null)
                {
                    return new ValidationResult("Internal server error occurred");
                }
                else
                {
                    var otherValue = property.GetValue(context.ObjectInstance, null);
                    if (!otherValue.Equals(value))
                        return new ValidationResult("Passwords do not match");
                }
            }

            return ValidationResult.Success;
        }
    }
}