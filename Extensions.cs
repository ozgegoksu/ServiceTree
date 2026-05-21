namespace ServiceTreeDemo;

public static class Extensions
{
    public static string F(this double d) => d.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

    public static string Color20(this ServiceTreeDemo.Models.ServiceProject _, string hex)
    {
        return hex + "22";
    }
}