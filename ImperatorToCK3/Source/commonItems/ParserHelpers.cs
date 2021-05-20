using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace commonItems
{
    public class SingleString : Parser
    {
        public SingleString(BufferedStreamReader sr) {
            GetNextTokenWithoutMatching(sr); // equals sign
            var nextToken = GetNextTokenWithoutMatching(sr);
            if (nextToken == null)
            {
                Log.WriteLine(LogLevel.Error, "SingleString: next token not found!"); ;
            }
            else
            {
                theString = RemQuotes(nextToken);
            }
        }
        public string GetString()
        {
            return theString;
        }
        string theString = "";
    }
}
