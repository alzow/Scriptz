namespace ScriptzApp.Models.Api.Responses;

public class PrescriptionResponse
{
    public string Id { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public DateTime PrescriptionDate { get; set; }
    public List<MedicationResponse> Medications { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
