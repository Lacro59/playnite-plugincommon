using System;
using System.Collections.Generic;

namespace PluginCommon
{
    public class CumulErrors
    {
        private List<string> ListMessage = new List<string>();

        /// <summary>
        /// Add a new error message.
        /// </summary>
        /// <param name="Message"></param>
        public void Add(string Message)
        {
            if (!ListMessage.Exists(x => x == Message))
            {
                ListMessage.Add(Message);
            }
        }

        /// <summary>
        /// Get list errors messages formatted.
        /// </summary>
        /// <returns></returns>
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
