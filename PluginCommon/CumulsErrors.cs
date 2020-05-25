using System;
using System.Collections.Generic;

namespace SuccessStory.Common
{
    class CumulErrors
    {
        private List<string> ListMessage = new List<string>();

        public void Add(string Message)
        {
            if (!ListMessage.Exists(x => x == Message))
            {
                ListMessage.Add(Message);
            }
        }

        public string Get()
        {
            string Result = "";
            for (int i = 0; i < ListMessage.Count; i++)
            {
                Result += ((i != 0) ? Environment.NewLine : "") + ListMessage[i];
            }
            return Result;
        }
    }
}
