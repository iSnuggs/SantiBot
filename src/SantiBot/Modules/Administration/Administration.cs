#nullable disable
namespace SantiBot.Modules.Administration;

public partial class Administration : SantiModule<AdministrationService>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public Administration(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
}