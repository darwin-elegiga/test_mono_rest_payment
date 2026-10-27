// See https://aka.ms/new-console-template for more information
using System.ComponentModel;

public class HostUriStringConverter : StringConverter
{

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

    public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
        List<string> list = new List<string>();
        list.Add("");
        list.Add("https://payment-routing-api.stg.pks.vpayusa.net");
        list.Add("https://payment-routing-api.dev.pks.vpayusa.net");
        list.Add("http://localhost:5001");
        return new StandardValuesCollection(list);
    }
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

}
