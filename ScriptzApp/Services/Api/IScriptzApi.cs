using Refit;
using ScriptzApp.Models.Api.Requests;
using ScriptzApp.Models.Api.Responses;

namespace ScriptzApp.Services.Api;

public interface IScriptzApi
{
    // Auth Endpoints
    [Post("/api/auth/login")]
    Task<AuthResponse> LoginAsync([Body] LoginRequest request);

    [Post("/api/auth/register")]
    Task<AuthResponse> RegisterAsync([Body] RegisterRequest request);

    [Post("/api/auth/refresh")]
    Task<AuthResponse> RefreshTokenAsync([Body] RefreshTokenRequest request);

    // Medications Endpoints
    [Get("/api/medications")]
    Task<List<MedicationResponse>> GetMedicationsAsync();

    [Get("/api/medications/{id}")]
    Task<MedicationResponse> GetMedicationByIdAsync(string id);

    [Post("/api/medications")]
    Task<MedicationResponse> CreateMedicationAsync([Body] CreateMedicationRequest request);

    [Put("/api/medications/{id}")]
    Task<MedicationResponse> UpdateMedicationAsync(string id, [Body] UpdateMedicationRequest request);

    [Delete("/api/medications/{id}")]
    Task DeleteMedicationAsync(string id);

    // Prescriptions Endpoints
    [Get("/api/prescriptions")]
    Task<List<PrescriptionResponse>> GetPrescriptionsAsync();

    [Get("/api/prescriptions/{id}")]
    Task<PrescriptionResponse> GetPrescriptionByIdAsync(string id);

    [Post("/api/prescriptions")]
    Task<PrescriptionResponse> CreatePrescriptionAsync([Body] CreatePrescriptionRequest request);

    [Put("/api/prescriptions/{id}")]
    Task<PrescriptionResponse> UpdatePrescriptionAsync(string id, [Body] UpdatePrescriptionRequest request);

    [Delete("/api/prescriptions/{id}")]
    Task DeletePrescriptionAsync(string id);

    // Reminders Endpoints
    [Get("/api/reminders")]
    Task<List<ReminderResponse>> GetRemindersAsync();

    [Get("/api/reminders/medication/{medicationId}")]
    Task<List<ReminderResponse>> GetRemindersByMedicationAsync(string medicationId);

    [Post("/api/reminders")]
    Task<ReminderResponse> CreateReminderAsync([Body] CreateReminderRequest request);

    [Put("/api/reminders/{id}")]
    Task<ReminderResponse> UpdateReminderAsync(string id, [Body] UpdateReminderRequest request);

    [Delete("/api/reminders/{id}")]
    Task DeleteReminderAsync(string id);

    // User Profile
    [Get("/api/user/profile")]
    Task<UserResponse> GetProfileAsync();

    [Put("/api/user/profile")]
    Task<UserResponse> UpdateProfileAsync([Body] UpdateProfileRequest request);
}
