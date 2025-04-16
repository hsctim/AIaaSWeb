using System;

namespace AIaaS.Nlp
{
    public class NlpWorkflowStateConsts
    {
        public const int MinStateNameLength = 1;
        public const int MaxStateNameLength = 256;

        public const int MinStateInstructionLength = 0;
        public const int MaxStateInstructionLength = 1024;

        public const int MinVectorLength = 0;
        public const int MaxVectorLength = 65536;

        public const int MinOutgoingFalseOpLength = 0;
        public const int MaxOutgoingFalseOpLength = 65536;

        public const int MinOutgoing3FalseOpLength = 0;
        public const int MaxOutgoing3FalseOpLength = 65536;


        static public readonly Guid WfsNull = new Guid("00000000-0000-0000-0000-000000000000");        //全部
        //static public readonly Guid WfsNotNull = new Guid("00000000-0000-0000-0000-000000000001");     //全部的非流程
        static public readonly Guid WfsKeepCurrent = new Guid("00000000-0000-0000-0000-000000000010"); //保持目前的流程狀態
    }
}