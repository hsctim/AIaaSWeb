//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using Abp.Application.Services.Dto;
//using AIaaS.MultiTenancy.Payments;

//namespace AIaaS.Editions.Dto
//{
//    public class NlpEditionLimitation
//    {
//        public const int MaxUserCount = 10;
//        public const int MinUserCount = 1;
//        public const int MaxChatbotCount = 10;
//        public const int MinChatbotCount = 1;
//        public const int MaxQuestionCount = 10000;
//        public const int MinQuestionCount = 1000;
//    }

//    public class NlpEditionDto
//    {
//        public int Id { get; set; }
//        public string Name { get; set; }
//        public string DisplayName { get; set; }
//        //public string DisplayNameWithFeatures { get; set; }
//        public int UserCount { get; set; }
//        public int ChatbotCount { get; set; }
//        public int QuestionCount { get; set; }
//        public bool IsFree { get; set; }

//    }
//}