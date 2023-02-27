namespace Io.Juenger.Autoconf.Tests
{
    public class MyTestConfig : IMyTestConfig
    {
        public int PropInt { get; set; }
        
        public string PropString { get; set; }
        
        public float PropFloat { get; set; }

        public bool PropBool { get; set; }
    }
}