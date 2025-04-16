using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIaaS.Nlp.Training
{
    public class NlpCbMSourceData
    {
        //public class NlpCbMSourceDataQA
        //{
        //    /// <summary>
        //    /// NNID
        //    /// </summary>
        //    public int i;

        //    /// <summary>
        //    /// Questions
        //    /// </summary>
        //    public string[] q { set; get; }

        //    /// <summary>
        //    /// Answers
        //    /// </summary>
        //    public string[] a { set; get; }
        //}

        public class NlpCbMSourceDataQA_DictionaryNNID
        {
            /// <summary>
            /// QAId
            /// </summary>
            //public Guid d;

            /// <summary>
            /// Questions
            /// </summary>
            public string[] q { set; get; }

            //public string q { set; get; }

            /// <summary>
            /// Answers
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string[] a { set; get; }

            /// <summary>
            /// workflowstatus
            /// </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public Guid? w { set; get; }
        }

        public Guid ChatbotId { set; get; }
        public DateTime StartTime { set; get; }
        public IDictionary<int, NlpCbMSourceDataQA_DictionaryNNID> QaSets { set; get; }

        //public IDictionary<string, int[]> wordVectorDictionary { get; set; }

        //public IDictionary<string, int[]> workflowDictionary { get; set; }
    }

    //public class NlpCbMTrainnedData
    //{
    //    public class NlpCbMTrainnedDataQA
    //    {
    //        /// <summary>
    //        /// NNID
    //        /// </summary>
    //        public int i;

    //        /// <summary>
    //        /// QAId
    //        /// </summary>
    //        public Guid d;

    //        /// <summary>
    //        /// Question Segments
    //        /// </summary>
    //        public List<List<string>> q { set; get; }

    //        /// <summary>
    //        /// Answers
    //        /// </summary>
    //        public List<string> a { set; get; }
    //    };

    //    public Guid ChatbotId { set; get; }
    //    public IQueryable<NlpCbMTrainnedDataQA> TrainnedTT { set; get; }
    //    public String TrainnedNN { set; get; }
    //};

    public class NlpCbMTrainingDataDTO
    {
        public Guid ModelId { set; get; }
        public string Status { set; get; }
        public NlpCbMSourceData SourceData { set; get; }
        //public NlpCbMTrainnedData TrainnedData { set; get; }

        public string Language { set; get; }

        public bool RebuildModel { get; set; }

        public bool IsFreeEdition { get; set; }
    };
}