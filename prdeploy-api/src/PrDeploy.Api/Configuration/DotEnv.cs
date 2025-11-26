namespace PrDeploy.Api.Configuration
{
    public class DotEnv
    {
        //// SourceRef: https://dusted.codes/dotenv-in-dotnet
        public static void Load(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            foreach (var line in File.ReadAllLines(filePath))
            {
                var separator = line.IndexOf("=");

                if (separator == -1)
                {
                    continue;
                }

                var name = line.Substring(0, separator);
                var value = line.Substring(separator + 1);

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
                {
                    continue;
                }

                Environment.SetEnvironmentVariable(name, value.Trim('"'));
            }
        }
    }
}
