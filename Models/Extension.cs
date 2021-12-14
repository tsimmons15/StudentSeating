using System;
using System.Collections.Generic;
using System.Text;

namespace StudentSeating.Models
{
    public static class Extension
    {
        public static string TrimExcess(this string s)
        {
            bool comma = false;
            StringBuilder ret = new StringBuilder();
            foreach (char c in s)
            {
                if (!comma && c == ',')
                {
                    comma = true;
                    ret.Append(c);
                }
                else if (char.IsLetter(c))
                {
                    ret.Append(c);
                }
            }

            return ret.ToString();
        }
    }
}
