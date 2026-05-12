namespace DTM.ORACLE
{
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text;
    using System.Text.Json.Serialization;
  
    public class REST : IDisposable
    {
        private readonly HttpClient _http;
        private readonly bool _ownsHandler;
        private readonly SocketsHttpHandler? _handler;
        public ServerCredential serverCredential { get; private set; }
        public REST(ServerCredential credential, bool trustAllCertificates = false)
        {
            serverCredential = credential;

            _handler = new SocketsHttpHandler();

            if (trustAllCertificates)
            {
                // ACHTUNG: deaktiviert MITM-Schutz. Siehe Hinweise unten zur Produktivvariante.
                _handler.SslOptions.RemoteCertificateValidationCallback =
                    (_, _, _, _) => true;
            }

            _http = new HttpClient(_handler, disposeHandler: true)
            {
                BaseAddress = new Uri($"https://{credential.Server}/ovirt-engine/api/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _ownsHandler = true;

            string basic = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{credential.User}:{credential.Password}"));
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", basic);

            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // Fixiert API-Major-Version. Empfohlen, falls Engine später aktualisiert wird.
            _http.DefaultRequestHeaders.Add("Version", "4");

            // OLVM antwortet bei fehlendem User-Agent mit 403 in manchen Konfigs
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("OlvmCsharpClient/1.0");
        }

        public async Task<IReadOnlyList<VmInfo>> GetVmsAsync(string? search = null, CancellationToken ct = default)
        {
            string url = "vms";
            if (!string.IsNullOrWhiteSpace(search))
            {
                url += $"?search={Uri.EscapeDataString(search)}";
            }

            VmListResponse? response = await _http.GetFromJsonAsync<VmListResponse>(url, ct);
            return response?.Vms ?? [];
        }

        /// <summary>Liefert (Name, FQDN, Status) je VM.</summary>
        public async Task<IReadOnlyList<VmFqdnEntry>> GetAllVmFqdnsAsync(bool onlyRunning = false, CancellationToken ct = default)
        {
            // Mit "status=up" sparst du Bandbreite, wenn dich nur laufende VMs interessieren.
            string? search = onlyRunning ? "status=up" : null;
            IReadOnlyList<VmInfo> vms = await GetVmsAsync(search, ct);

            return vms
                .Select(v => new VmFqdnEntry(v.Name, v.Fqdn, v.Status, v.Id))
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        public void Dispose()
        {
            _http.Dispose();
            if (_ownsHandler) _handler?.Dispose();
        }
    }
    public sealed record VmListResponse(
    [property: JsonPropertyName("vm")] List<VmInfo> Vms);

    public sealed record VmInfo(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("fqdn")] string? Fqdn,
        [property: JsonPropertyName("status")] string Status);

    public sealed record VmFqdnEntry(string Name, string? Fqdn, string Status, string Id);
}