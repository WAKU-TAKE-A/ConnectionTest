using System;

namespace ConnectionTest.Models
{
    public class InitIp
    {
        public int Ip0 = 192;
        public int Ip1 = 168;
        public int Ip2 = 0;
        public int Ip3 = 0;
        public int Msk0 = 255;
        public int Msk1 = 255;
        public int Msk2 = 255;
        public int Msk3 = 0;
        public int Gate0 = 0;
        public int Gate1 = 0;
        public int Gate2 = 0;
        public int Gate3 = 0;
        public int Ip0dst = 192;
        public int Ip1dst = 168;
        public int Ip2dst = 0;
        public int Ip3dst = 0;

        public InitIp()
        {
            // do nothing
        }

        public InitIp Clone()
        {
            // Object型で返ってくるのでキャストが必要
            return (InitIp)MemberwiseClone();
        }
    }
}
