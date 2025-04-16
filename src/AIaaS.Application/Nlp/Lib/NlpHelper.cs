using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AIaaS.Nlp.Lib
{
    public class NlpHelper
    {
        public static int[] CreateNewVector()
        {
            int[] wordVector = new int[300];
            Thread.Sleep(1);
            Random rand = new Random();

            for (int n = 0; n < wordVector.Length; n++)
                wordVector[n] = rand.Next(-1000, 1000) / rand.Next(1, 6);
            return wordVector;
        }
    }
}
