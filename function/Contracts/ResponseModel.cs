using System.Text.Json.Serialization;

namespace az_fx_k8s_admission_control.Contracts
{
    public class ResponseModel
    {
        public ResponseModel(string uid, bool isAllowed, string result = "")
        {
            Response = new Response {Allowed = isAllowed, Uid = uid, Status = new Status {Message = result}};
        }

        [JsonPropertyName("apiversion")] public string ApiVersion { get; } = "admission.k8s.io/v1";

        [JsonPropertyName("kind")] public string Kind { get; } = "AdmissionReview";

        [JsonPropertyName("response")] public Response Response { get; set; }
    }
}