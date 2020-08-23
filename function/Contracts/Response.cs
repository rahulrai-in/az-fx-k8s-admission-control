using System.Text.Json.Serialization;

namespace az_fx_k8s_admission_control.Contracts
{
    public class Response
    {
        [JsonPropertyName("uid")] public string Uid { get; set; }

        [JsonPropertyName("allowed")] public bool Allowed { get; set; }

        [JsonPropertyName("status")] public Status Status { get; set; }
    }
}