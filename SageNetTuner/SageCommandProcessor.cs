namespace SageNetTuner
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Timers;

    using Autofac;

    using NLog;

    using SageNetTuner.Configuration;
    using SageNetTuner.Contracts;
    using SageNetTuner.Filters;
    using SageNetTuner.Model;
    using SageNetTuner.Network;

    using Tamarack.Pipeline;

    public class SageCommandProcessor
    {

        private readonly Logger Logger;

        private readonly ILifetimeScope _lifetimeScope;

        private readonly TunerElement _tunerSettings;

        private readonly DeviceElement _deviceSettings;

        private Lineup _lineup;

        private readonly ICaptureManager _executableProcessCapture;

        private readonly IChannelProvider _channelProvider;


        public SageCommandProcessor(ILifetimeScope lifetimeScope, TunerElement tunerSettings, DeviceElement deviceSettings, ICaptureManager executableProcessCaptureManager, IChannelProvider channelProvider, Logger logger)
        {
            _lifetimeScope = lifetimeScope;
            _tunerSettings = tunerSettings;
            _deviceSettings = deviceSettings;

            _executableProcessCapture = executableProcessCaptureManager;
            _channelProvider = channelProvider;
            Logger = logger;
        }

        public string Name
        {
            get
            {
                return _tunerSettings.Name;
            }
        }

        public void OnError(TcpServer server, Exception e)
        {
            Logger.Error("TcpServer.OnError", e);
        }

        public void OnDataAvailable(TcpServerConnection connection)
        {
            Logger.Trace("TcpServer.OnDataAvailable: {0}", connection.ClientAddress.ToString());

            var client = connection.Socket;
            using (var stream = client.GetStream())
            using (var sw = new StreamWriter(stream))
            {

                if (stream.DataAvailable)
                {
                    var data = new byte[5013];

                    var stringBuilder = new StringBuilder();

                    try
                    {
                        do
                        {
                            var bytesRead = stream.Read(data, 0, data.Length);

                            Logger.Debug("{1}:{2} Bytes Received: {0}", bytesRead, connection.ClientAddress, _tunerSettings.ListenerPort);

                            stringBuilder.Append(Encoding.UTF8.GetString(data, 0, bytesRead));
                            string str = stringBuilder.ToString();

                            if (!string.IsNullOrEmpty(str) && str.Length > 2 && str.Substring(str.Length - 2) == "\r\n")
                            {
                                var response = HandleRequest(stringBuilder.ToString().Trim());
                                Logger.Debug("Sending:{0}", response);
                                sw.Write(response + Environment.NewLine);
                            }
                        }
                        while (client.Available > 0);
                    }

                    catch (IOException ex)
                    {
                        Logger.Warn("IOException while reading stream", ex);
                    }

                }
            }
        }

        public void OnConnect(TcpServerConnection connection)
        {
            Logger.Trace("TcpServer.OnConnect: {0}", connection.ClientAddress.ToString());
        }

        private string HandleRequest(string request)
        {

            var context = new RequestContext(request)
            {
                Settings =
                {
                    Device = _deviceSettings,
                    Tuner = _tunerSettings,
                    Lineup = _lineup,
                }
            };


            using (var innerScope = _lifetimeScope.BeginLifetimeScope("request", (builder)=>
            {
                builder.RegisterInstance(Logger);
                builder.RegisterInstance(context);
                
            }))
            {

                Logger.Info("HandleRequest: [{0}]", request);
                try
                {


                    var pipeline = new Pipeline<RequestContext, string>((IServiceProvider)innerScope)
                            .Add<LoggingFilter>()
                            .Add<ParseRequestFilter>()
                            .Add<NoopFilter>()
                            .Add<GetFileSizeFilter>()
                            .Add<StartFilter>()
                            .Add<StopFilter>()
                            .Add<VersionFilter>()
                            .Add<GetSizeFilter>()
                            .Finally(
                                c =>
                                {
                                    Logger.Warn("Unknown Command: {0}", c.Request);
                                    return "ERROR Unknown Command";
                                });


                    return pipeline.Execute(context);


                }
                catch (Exception ex)
                {
                    Logger.Error("Exception handling request", ex);
                    return "ERROR " + ex.Message;
                }

            }

        }




        public void Initialize()
        {

            _lineup = _channelProvider.GetLineup(_deviceSettings);

            if (_lineup == null)
                Logger.Warn("Channel Lineup not retrieved.");
            else if (_lineup.Channels.Count > 0)
                Logger.Info("Retrieved Channels: Count=[{0}]", _lineup.Channels.Count);



        }

        public void StopListening()
        {
            _executableProcessCapture.Stop();
        }
    }
}