using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AIaaS.Nlp
{
    public class UserFriendlyExceptionCode
    {
        public const int Success = 0;
        public const int ExceptionCodeError = -1;
        public const int AccessTokenError = -2;
        public const int NlpCbTrainingStatusError = -1000;

        private static UserFriendlyExceptionCode thisClass = new UserFriendlyExceptionCode();

        static public int Code(string codeName)
        {
            try
            {
                int codeNumber = (int)thisClass.GetType().GetField(codeName).GetValue(thisClass);
                return codeNumber;
            }
            catch (Exception)
            {
                return ExceptionCodeError;
            }
        }
    }
}