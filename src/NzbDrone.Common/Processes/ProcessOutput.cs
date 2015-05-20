using System;
using System.Collections.Generic;

namespace NzbDrone.Common.Processes
{
    public class ProcessOutput
    {
        public int ExitCode { get; set; }
        public List<String> Standard { get; private set; }
        public List<String> Error { get; private set; }

        public ProcessOutput()
        {
            Standard = new List<string>();
            Error = new List<string>();
        }
    }
}
