namespace VPay.Gateway.Application;

public class PreferenceLookupDtoList : List<PreferenceLookupDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public const string ValidationPropertyName = "items";
}
