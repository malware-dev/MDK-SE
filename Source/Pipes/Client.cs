using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Pipes
{
    public class Client : IDisposable
    {
        readonly string _pipeName;
        readonly string _expectedIdentity;
        readonly SynchronizationContext _synchronizationContext;

        public Client(string pipeName, string expectedIdentity, SynchronizationContext synchronizationContext = null)
        {
            _pipeName = pipeName;
            _expectedIdentity = expectedIdentity;
            _synchronizationContext = synchronizationContext;
        }

        //private Client()
        //{
        //    NamedPipeClientStream pipeClient =
        //        new NamedPipeClientStream(".", "testpipe",
        //            PipeDirection.InOut, PipeOptions.None,
        //            TokenImpersonationLevel.Impersonation);
        //}

        public async Task<string> Request(string transmitMessage)
        {
            using (var stream = new NamedPipeClientStream(".", _expectedIdentity, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Anonymous))
            {
                var message = new MyCommandLine();
                stream.ReadTimeout = 500;
                stream.WriteTimeout = 500;
                await stream.ConnectAsync(500);
                var writer = new StreamWriter(stream);
                var reader = new StreamReader(stream);
                var response = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(response) || response != $"ID {_expectedIdentity}")
                    throw new ClientException("Bad response");
                writer.WriteLine($"REQUEST {transmitMessage}");
                response = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(response))
                    throw new ClientException("Bad response");
                var code = response.Substring(0, Math.Max(response.Length, response.IndexOf(' ')));

            }
            throw new NotImplementedException();
        }

        public void Dispose()
        { }
    }
}