using System.Text.Json.Serialization;

namespace az_fx_k8s_admission_control.Contracts
{
    public class Status
    {
        [JsonPropertyName("message")] public string Message { get; set; }
    }
}