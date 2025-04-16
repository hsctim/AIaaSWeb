using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AIaaS.Nlp.Dtos.NlpQA.ValidationAttribute
{
    public class StringArrayMaxLengthAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public int MaximumLength { get; }

        public StringArrayMaxLengthAttribute(int maximumLength)
        {
            MaximumLength = maximumLength;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            IEnumerable<string> array = value as IEnumerable<string>;

            if (array == null)
                return ValidationResult.Success;

            foreach (var str in array)
            {
                if (str.Length > MaximumLength)
                    return new ValidationResult("NlpQA_MoreThanMaxStringLength");
            }

            return ValidationResult.Success;
        }
    }
}