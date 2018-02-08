using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MediaBackupManager.View
{
    /// <summary>
    /// Validates that a string is not null or empty. </summary>
    public class StringNotEmptyRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if(!(value is string))
                return new ValidationResult(false, "Value must be a string.");

            if(string.IsNullOrWhiteSpace(value.ToString()))
                return new ValidationResult(false, "Value is null or empty.");

            return ValidationResult.ValidResult;
        }
    }

}
