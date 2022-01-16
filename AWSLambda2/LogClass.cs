using System;
using System.Collections.Generic;
using System.Text;

namespace AWSLambda2
{
    public class Log
    {
        public int iconUser { get; set; }
        public string User { get; set; }
        public DateTime date { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public bool verified { get; set; }
    }
}
