
using BicepNet.Core;
using BicepNet.Core.Configuration;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
		.AddConsole();
});
ILogger logger = loggerFactory.CreateLogger<Program>();
BicepWrapper bicepWrapper = new(logger);

Console.WriteLine(string.Join(',',args));

if (args.Length > 0)
{
    switch (args[0].ToLower())
    {
        case "build":
            if (string.IsNullOrEmpty(args[1]))
                throw new ArgumentException("Missing template path");

            var buildResult = bicepWrapper.Build(args[1]);
            foreach (var item in buildResult)
            {
                Console.WriteLine(item);
            }
            break;
        case "exportresource":
            if (string.IsNullOrEmpty(args[1]))
                throw new ArgumentException("Missing resource id");

            var exportResult = bicepWrapper.ExportResources(new[] { args[1] });
            Console.WriteLine(exportResult);
            break;
        case "converttobicep":
            if (string.IsNullOrEmpty(args[1]))
                throw new ArgumentException("Missing resource id");
            if (string.IsNullOrEmpty(args[2]))
                throw new ArgumentException("Missing resource body");

            var body = System.IO.File.ReadAllText(args[2]);
            var convertResult = bicepWrapper.ConvertResourceToBicep(args[1], body);
            Console.WriteLine(convertResult);
            break;
        case "config":
            if (string.IsNullOrEmpty(args[1]))
                throw new ArgumentException("Missing scope");
            if (!Enum.TryParse(args[1], out BicepConfigScope scope)) {
                throw new ArgumentException($"Invalid scope: ${args[1]}");
            }
            var path = args.Length < 3 || string.IsNullOrEmpty(args[2]) ? "" : args[2];
            Console.WriteLine(bicepWrapper.GetBicepConfigInfo(scope, path).Config);
            break;
        default:
			break;
	}
} 