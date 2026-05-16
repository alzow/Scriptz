namespace ScriptzApp.Models.Api.Requests;

public class CreatePrescriptionRequest
{
    public string DoctorName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public DateTime PrescriptionDate { get; set; }
    public List<string> MedicationIds { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

public class UpdatePrescriptionRequest : CreatePrescriptionRequest
{
}
