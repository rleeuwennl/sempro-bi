using System;
using System.ServiceModel;
using System.Web.Http.SelfHost;
using System.ServiceModel.Channels;
using System.Web.Http.SelfHost.Channels;

internal class ExtendHttpSelfHostConfiguration : HttpSelfHostConfiguration
{
    public ExtendHttpSelfHostConfiguration(Uri baseAddress) : base(baseAddress)
    {
    }
    protected override BindingParameterCollection OnConfigureBinding(HttpBinding httpBinding)
    {
        httpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
        httpBinding.Security.Mode = HttpBindingSecurityMode.Transport;
        httpBinding.MaxReceivedMessageSize = long.MaxValue;
        this.MaxBufferSize = int.MaxValue;
        this.MaxReceivedMessageSize = long.MaxValue;

        return base.OnConfigureBinding(httpBinding);
    }
}
