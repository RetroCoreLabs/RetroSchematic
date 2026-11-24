//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using RetroSchematic.Model;

namespace RetroSchematic.Mcp
{
    /// <summary>
    /// Start-up: an MCP server on standard input and output, with the sheet tools.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Runs the server until the client closes standard input.
        /// </summary>
        /// <param name="args">
        /// Passed to the host.
        /// </param>
        /// <returns>
        /// A task that ends when the server stops.
        /// </returns>
        public static async Task Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            // standard output carries the protocol; nothing else may be written there
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
            // which build this is, on standard error (standard output is the protocol) and in the
            // server info a client sees at the handshake - from the build stamp, see
            // RetroSchematic.Model/BuildIdentity.cs
            Console.Error.WriteLine(BuildIdentity.Describe("retroschematic-mcp"));
            builder.Services.AddMcpServer(o =>
                {
                    o.ServerInfo = new Implementation
                    {
                        Name = "retroschematic-mcp",
                        Version = BuildIdentity.Version + "+" + BuildIdentity.Commit + (BuildIdentity.IsDirty ? "+" : ""),
                        Title = BuildIdentity.Describe("RetroSchematic MCP server"),
                    };
                })
                .WithStdioServerTransport()
                .WithTools(new SheetTools(AppContext.BaseDirectory));
            await builder.Build().RunAsync();
        }
    }
}
