using System;
using Abp.Application.Services.Dto;

namespace AIaaS.Nlp.Dtos
{
    public enum NlpWfsFalsePredictionOpSelect
    {
        None = 0,        //不處理
        Forward = 1,    //至指定步驟
        Exit = 2        //退出
    };

    public class NlpWfsFalsePredictionOpDto
    {
        //是否要回應Message
        //public bool ResponseMsg;

        //回應的Message
        public string ResponseMsg;

        //public NlpWfsFalsePredictionOpSelect Operation;

        public Guid NextStatus;
        //00000000-0000-0000-0000-000000000000 : Exit;    
        //00000000-0000-0000-0000-000000000001 : Current;
    }
}

