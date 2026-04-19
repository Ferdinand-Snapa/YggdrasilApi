namespace YggdrasilApi.Utils;

public static class TypeCompatibility
{
    private static readonly Dictionary<string, string[]> CompatabiltyDict = new Dictionary<string, string[]>
    {
        //inputType -> outputType
        {"number", ["int", "float"]},
    };
    public static bool IsCompatible(string outputType, string inputType)
    {
        //in accepts all
        if (inputType == "any") return true;
        //in and out are the same
        if (inputType == outputType) return true;

        string[] compatible = CompatabiltyDict.GetValueOrDefault(inputType) ?? Array.Empty<string>();
        if (compatible.Length == 0) Console.WriteLine("Warning: No compatibility rules defined for input type '" + inputType + "'. Only exact matches and 'any' will be accepted.");

        return compatible.Contains(outputType);
    }
}
