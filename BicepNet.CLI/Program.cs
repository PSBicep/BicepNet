
using BicepNet.Core;
using Microsoft.Extensions.Logging;


using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
		.AddConsole();
});
ILogger logger = loggerFactory.CreateLogger<Program>();
BicepWrapper.Initialize(logger);

Console.WriteLine(string.Join(',',args));

if (args.Length > 0)
{
	switch (args[0].ToLower())
	{
		case "exportresource":
			if (string.IsNullOrEmpty(args[1]))
				throw new ArgumentException("Missing resource id");

			var result = BicepWrapper.ExportResources(new[] { args[1] });
			Console.WriteLine(result);
			break;
        default:
			break;
	}
} 