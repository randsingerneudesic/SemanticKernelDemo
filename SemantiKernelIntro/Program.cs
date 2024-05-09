using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Experimental.Orchestration;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using Microsoft.SemanticKernel.Memory;
using SemantiKernelIntro;
using Xunit;
using Microsoft.SemanticKernel.Planning.Handlebars;
using System.ComponentModel;
using Newtonsoft.Json;
using Microsoft.Graph.Models;
#pragma warning disable SKEXP0060

public class DatePlugin
{
    [KernelFunction, Description("Displays the current date.")]
    public static string GetCurrentDate()
        => DateTime.Today.ToLongDateString();

    [KernelFunction, Description("Displays the current time.")]
    public static string GetTime()
        => DateTime.Now.ToShortTimeString();
}
public class StepSummary
{
    public string? Thought { get; set; }
    public string? Action { get; set; }
    public string? Observation { get; set; }
}


public class Program
{
    public static async Task Main(string[] args)
    {
        IKernelBuilder builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
                       settings.OpenAIDeploymentName,
                                  settings.OpenAIEndpoint,
                                             settings.OpenAIAPIKey);
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        Kernel kernel = builder.Build();

        builder.Services.AddSingleton(loggerFactory);
        string batTemplate = @"
        Respond to the user's request as if you were Batman. Be creative and funny, but keep it clean.
        Try to answer user questions to the best of your ability.

        User: How are you?
        AI: I'm fine. It's another wonderful day of inflicting vigilante justice upon the city.

        User: Where's a good place to shop for books?
        AI: You know who likes books? The Riddler. You're not the Riddler, are you?

        User: {{$input}}Hi 
        AI: 
        ";
        string alfredTemplate = @"
        Respond to the user's request as if you were Alfred, butler to Bruce Wayne. 
        Your job is to summarize text from Batman and relay it to the user. 
        Be polite and helpful, but a little snark is fine.

        Batman: I am vengeance. I am the night. I am Batman!
        AI: The dark knight wishes to inform you that he remains the batman.

        Batman: The missing bags - WHERE ARE THEY???
        AI: It is my responsibility to inform you that Batman requires information on the missing bags most urgently.

        Batman: {{$input}}
        AI: 
        ";

        KernelFunction batFunction =
            kernel.CreateFunctionFromPrompt(batTemplate,
                                                     description: "Responds to queries in the voice of Batman");
        KernelFunction alfredFunction =
            kernel.CreateFunctionFromPrompt(alfredTemplate,
                                                     description: "Alfred, butler to Bruce Wayne. Summarizes responses politely.");
        /*Console.WriteLine("Enter your message to Batman so he can relay it to Alfred");
        string message = Console.ReadLine();
        if (message == null)
        {
            message = "I'm Batman";
        }
        KernelArguments kernelArgs = new KernelArguments { ["userMessage"] = message };*/
        kernel.ImportPluginFromFunctions(pluginName: "bat2alfred", functions: new List<KernelFunction> { batFunction, alfredFunction });
        kernel.ImportPluginFromType<DatePlugin>("CheckTheDate");
        FunctionCallingStepwisePlanner planner = new FunctionCallingStepwisePlanner(new FunctionCallingStepwisePlannerOptions() {});
        Task<FunctionCallingStepwisePlannerResult> task = planner.ExecuteAsync(kernel, "Tell Batman the current time and have Alfred summarize his response");//, kernelArgs);
        FunctionCallingStepwisePlannerResult result = await task;
        
        foreach (ChatMessageContent step in result.ChatHistory.ToList<ChatMessageContent>())
        {
            Console.WriteLine(step);
            Console.WriteLine();
        }
        
    }
}