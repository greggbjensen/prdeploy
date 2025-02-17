﻿namespace PrDeploy.Api.Configuration
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
                var parts = line.Split(
                    '=',
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                    continue;

                Environment.SetEnvironmentVariable(parts[0], parts[1].Trim('"'));
            }
        }
    }
}
