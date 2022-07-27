using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemicalConsensusBot
{
    public static class Extensions
    {
        public static string Escape(this string str) => str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        public static string Unescape(this string str) => str.Replace("$gt;", ">").Replace("&lt;", "<").Replace("$amp;", "&");
    }
}
