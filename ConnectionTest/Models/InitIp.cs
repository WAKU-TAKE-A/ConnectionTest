namespace ConnectionTest.Models;

public class InitIp
{
    public int Ip0 { get; set; } = 192;
    public int Ip1 { get; set; } = 168;
    public int Ip2 { get; set; } = 0;
    public int Ip3 { get; set; } = 0;
    public int Msk0 { get; set; } = 255;
    public int Msk1 { get; set; } = 255;
    public int Msk2 { get; set; } = 255;
    public int Msk3 { get; set; } = 0;
    public int Gate0 { get; set; } = 0;
    public int Gate1 { get; set; } = 0;
    public int Gate2 { get; set; } = 0;
    public int Gate3 { get; set; } = 0;
    public int Ip0dst { get; set; } = 192;
    public int Ip1dst { get; set; } = 168;
    public int Ip2dst { get; set; } = 0;
    public int Ip3dst { get; set; } = 0;

    public InitIp Clone() => (InitIp)MemberwiseClone();
}
