﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using skUnit.Exceptions;
using skUnit.Parsers;
using Xunit.Abstractions;

namespace skUnit.Tests.SemanticKernel.TextScenarioTests
{
    public class SentimentFunctionTests
    {
        private string _apiKey;
        private string _endpoint;
        private Kernel Kernel { get; set; }
        private KernelFunction SentimentFunction { get; set; }
        private ITestOutputHelper Output { get; set; }

        public SentimentFunctionTests(ITestOutputHelper output)
        {
            Output = output;
            _apiKey = Environment.GetEnvironmentVariable("openai-api-key", EnvironmentVariableTarget.User) ??
                      throw new Exception("No ApiKey in environment variables.");
            _endpoint = Environment.GetEnvironmentVariable("openai-endpoint", EnvironmentVariableTarget.User) ??
                        throw new Exception("No Endpoint in environment variables.");

            SemanticKernelAssert.Initialize(_endpoint, _apiKey);

            Kernel = CreateKernel();
            SentimentFunction = Kernel.CreateFunctionFromPrompt(@"""
                What is sentiment of this input text, your options are: {{$options}}
                
                [[input text]]
                {{$input}}
                [[end of input text]]
                
                just result the sentiment without any spaces.

                SENTIMENT: 
                """);
        }

        Kernel CreateKernel()
        {
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion("gpt-35-turbo-test", _endpoint, _apiKey);

            var kernel = builder.Build();
            return kernel;
        }

        [Fact]
        public async Task Angry_True_MustWork()
        {
            var testContent = await LoadTextTestAsync("SentimentAngry_Complex");
            var tests = TextScenarioParser.Parse(testContent, "");

            foreach (var test in tests)
            {
                await SemanticKernelAssert.TestScenarioOnFunction(Kernel, SentimentFunction, test);
            }
        }

        [Fact]
        public async Task Angry_False_MustWork()
        {
            var testContent = await LoadTextTestAsync("SentimentHappy");
            var tests = TextScenarioParser.Parse(testContent, "");

            foreach (var test in tests)
            {
                var exception = await Assert.ThrowsAsync<SemanticAssertException>(() => SemanticKernelAssert.TestScenarioOnFunction(Kernel, SentimentFunction, test));
                Output.WriteLine($"""
                    EXCEPTION MESSAGE:
                    {exception.Message}
                    """);
            }
        }

        private async Task<string> LoadTextTestAsync(string test)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"skUnit.Tests.SemanticKernel.TextScenarioTests.Samples.{test}.sktest.txt";
            await using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            var result = await reader.ReadToEndAsync();
            return result ?? "";
        }
    }
}
