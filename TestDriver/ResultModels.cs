namespace Gb18030.TestDriver
{
    public class ControlTestResult
    {
        public string Page { get; set; }
        public string ControlId { get; set; }
        public int StringIndex { get; set; }
        public string EncodingName { get; set; }
        public string Expected { get; set; }
        public string Actual { get; set; }
        public bool Passed { get; set; }
        public string Message { get; set; }
    }
}