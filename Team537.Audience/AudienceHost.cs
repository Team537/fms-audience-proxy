using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Team537.Audience
{
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    using FMS.Contract.Service;
    using FMS.Infrastructure;
    using FMS.Infrastructure.Services;

    using Microsoft.Practices.Composite.Logging;

    public class AudienceHost
    {
        private ServiceHost _publishServiceHost;

        private ServiceHost _subscriptionManagerHost;

        private ILoggerFacade _logger;

        public AudienceHost(ILoggerFacade logger)
        {
            this._logger = logger;
        }

        public void Inititalize()
        {
            this._publishServiceHost = new ServiceHost(typeof(MessagePublishService), new Uri[0]);
            this._subscriptionManagerHost = new ServiceHost(typeof(MessageSubscriptionService), new Uri[0]);
        }

        public void Start(string publishUrl, string subscribeUrl)
        {
            try
            {
                this.Inititalize();
                NetTcpBinding netTcpBinding = new NetTcpBinding(SecurityMode.None);
                netTcpBinding.ReceiveTimeout = TimeSpan.MaxValue;
                netTcpBinding.MaxBufferSize = 2000000;
                netTcpBinding.MaxReceivedMessageSize = 2000000L;
                this._publishServiceHost.AddServiceEndpoint(typeof(IMessageEvents), (Binding)netTcpBinding, publishUrl);
                this._publishServiceHost.Open();

                new WSDualHttpBinding(WSDualHttpSecurityMode.None).ReceiveTimeout = TimeSpan.MaxValue;

                Binding binding = (Binding)new NetTcpBinding(SecurityMode.None);
                binding.ReceiveTimeout = TimeSpan.MaxValue;
                new NetNamedPipeBinding(NetNamedPipeSecurityMode.None).ReceiveTimeout = TimeSpan.MaxValue;
                this._subscriptionManagerHost.AddServiceEndpoint(
                    typeof(IMessageSubscriptionService),
                    binding,
                    subscribeUrl);
                this._subscriptionManagerHost.Open();
            }
            catch (Exception ex)
            {
                EntLibLoggerExtensions.Log(this._logger, "Error trying to start the message broker service", ex);
            }
        }

        public void Stop()
        {
            this._publishServiceHost.Close();
            this._subscriptionManagerHost.Close();
        }
    }
}
