using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemicalConsensusBot
{
    public static class Extensions
    {
        public static string Escape(this string str) => str.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
