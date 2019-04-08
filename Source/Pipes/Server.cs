using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;

namespace Pipes
{
    public class Server : IDisposable
    {
        public static Server Start(string pipeName, string identity, SynchronizationContext synchronizationContext = null, int instanceCount = 4)
        {
            var messager = new Server(pipeName, identity, synchronizationContext ?? SynchronizationContext.Current);
            messager.Start();
            return messager;
        }

        readonly CancellationTokenSource _cancellation;
        readonly string _pipeName;
        readonly string _identity;
        readonly SynchronizationContext _synchronizationContext;
        Task[] _tasks;

        Server(string pipeName, string identity, SynchronizationContext synchronizationContext)
        {
            _cancellation = new CancellationTokenSource();
            _pipeName = pipeName;
            _identity = identity;
            _synchronizationContext = synchronizationContext;
        }

        public event Func<Server, MyCommandLine, string> RequestReceived;

        void Start()
        {
            _tasks = new[]
            {
                new Task(ServerFunction, TaskCreationOptions.LongRunning),
                new Task(ServerFunction, TaskCreationOptions.LongRunning),
                new Task(ServerFunction, TaskCreationOptions.LongRunning),
                new Task(ServerFunction, TaskCreationOptions.LongRunning)
            };
        }

        public void Dispose()
        { }

        async void ServerFunction()
        {
            while (true)
            {
                using (var stream = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 4, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    stream.ReadTimeout = 500;
                    stream.WriteTimeout = 500;
                    var message = new MyCommandLine();
                    await stream.WaitForConnectionAsync(_cancellation.Token);
                    if (_cancellation.IsCancellationRequested)
                        return;

                    var writer = new StreamWriter(stream);
                    var reader = new StreamReader(stream);

                    writer.WriteLine($"ID \"{_identity}\"");
                    var messageSrc = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(messageSrc) || !message.TryParse(messageSrc))
                    {
                        writer.WriteLine("400");
                        continue;
                    }

                    var completion = new TaskCompletionSource<string>();
                    _synchronizationContext.Post(OnRequest, new Request(message, completion));
                    try
                    {
                        var response = completion.Task.Result;
                        if (response == null)
                            writer.WriteLine("400");
                        else
                            writer.WriteLine($"200 {response}");
                    }
                    catch (Exception exception)
                    {
                        void logError(object state)
                        {
                            MyLog.Default.WriteLine("MDKListenerPlugin Error");
                            MyLog.Default.WriteLine((Exception)state);
                        }

                        _synchronizationContext.Post(logError, exception);
                        writer.WriteLine("500");
                    }
                }
            }
        }

        void OnRequest(object state) => OnRequest((Request)state);

        protected virtual void OnRequest(Request state)
        {
            try
            {
                state.RespondWith(RequestReceived?.Invoke(this, state.Message));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public class Request
        {
            readonly TaskCompletionSource<string> _respond;

            public Request(MyCommandLine message, TaskCompletionSource<string> respond)
            {
                _respond = respond;
                Message = message;
            }

            public MyCommandLine Message { get; }

            public void RespondWith(string message)
            {
                _respond.SetResult(message);
            }

            public void Fail(Exception exception)
            {
                _respond.SetException(exception);
            }
        }

        //void Server()
        //{

        //    int threadId = Thread.CurrentThread.ManagedThreadId;

        //    // Wait for a client to connect
        //    pipeServer.WaitForConnection();
        //    pipeServer.WaitForConnectionAsync()


        //    Console.WriteLine("Client connected on thread[{0}].", threadId);
        //    try
        //    {
        //        // Read the request from the client. Once the client has
        //        // written to the pipe its security token will be available.

        //        StreamString ss = new StreamString(pipeServer);

        //        // Verify our identity to the connected client using a
        //        // string that the client anticipates.

        //        ss.WriteString("I am the one true server!");
        //        string filename = ss.ReadString();

        //        // Read in the contents of the file while impersonating the client.
        //        ReadFileToStream fileReader = new ReadFileToStream(ss, filename);

        //        // Display the name of the user we are impersonating.
        //        Console.WriteLine("Reading file: {0} on thread[{1}] as user: {2}.",
        //            filename, threadId, pipeServer.GetImpersonationUserName());
        //        pipeServer.RunAsClient(fileReader.Start);
        //    }
        //    // Catch the IOException that is raised if the pipe is broken
        //    // or disconnected.
        //    catch (IOException e)
        //    {
        //        Console.WriteLine("ERROR: {0}", e.Message);
        //    }
        //    pipeServer.Close();
        //}
    }
}