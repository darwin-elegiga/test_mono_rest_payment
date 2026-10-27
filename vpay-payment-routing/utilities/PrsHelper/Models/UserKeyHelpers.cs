// See https://aka.ms/new-console-template for more information
internal static class UserKeyHelpers
{

    private static List<string> _existingUserKeys = new List<string>();
    public static string GetUserKey(string prefix = "")
    {
        if (prefix.Length > 11)
        {
            throw new ArgumentException("UserKeyPrefix can be no more than 10 characters", nameof(prefix));

        }
        while (true)
        {
            var rando = RandomString(25 - prefix.Length);
            var userKey = $"{prefix}{rando}";
            if (!_existingUserKeys.Contains(userKey))
            {
                _existingUserKeys.Add(userKey);
                return userKey;
            }
        }
    }

    public static string RandomString(int length)
    {
        Random rand = new Random(DateTime.Now.Millisecond);
        string charbase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, length)
               .Select(_ => charbase[rand.Next(charbase.Length)])
               .ToArray());
    }
}