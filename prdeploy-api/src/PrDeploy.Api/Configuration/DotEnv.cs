namespace PrDeploy.Api.Configuration
{
    /// <summary>
    /// Represents dot env data used by the application.
    /// </summary>
    public class DotEnv
    {
        //// SourceRef: https://dusted.codes/dotenv-in-dotnet
        /// <summary>
        /// Performs the load operation.
        /// </summary>
        /// <param name="filePath">Text used as the file path.</param>
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
