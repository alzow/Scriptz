using ScriptzApp.Models.Api.Requests;
using ScriptzApp.Models.Api.Responses;
using ScriptzApp.Services.Api;

namespace ScriptzApp.Services.Stubs;

/// <summary>
/// In-memory stub that mirrors the prototype data so the UI can be fully tested
/// without a running backend. Registered instead of the real Refit client in DEBUG builds.
/// </summary>
public class StubScriptzApi : IScriptzApi
{
    // ── Seed data ─────────────────────────────────────────────────────────────

    private readonly List<MedicationResponse> _medications = new()
    {
        new()
        {
            Id          = "med-1",
            Name        = "Metformin 500mg",
            GenericName = "Metformin Hydrochloride",
            Dosage      = "500mg",
            Form        = "Tablet",
            Frequency   = "Twice daily",
            Instructions= "Take with food",
            StartDate   = DateTime.Today.AddDays(-26),
            EndDate     = null,
            IsActive    = true,
            CreatedAt   = DateTime.Today.AddDays(-30),
        },
        new()
        {
            Id          = "med-2",
            Name        = "Lisinopril 10mg",
            GenericName = "Lisinopril",
            Dosage      = "10mg",
            Form        = "Tablet",
            Frequency   = "Once daily",
            Instructions= "Take in the morning",
            StartDate   = DateTime.Today.AddDays(-19),
            EndDate     = null,
            IsActive    = true,
            CreatedAt   = DateTime.Today.AddDays(-20),
        },
        new()
        {
            Id          = "med-3",
            Name        = "Atorvastatin 20mg",
            GenericName = "Atorvastatin Calcium",
            Dosage      = "20mg",
            Form        = "Tablet",
            Frequency   = "Once daily",
            Instructions= "Take at night",
            StartDate   = DateTime.Today.AddDays(-60),
            EndDate     = DateTime.Today.AddDays(-5),
            IsActive    = false,
            CreatedAt   = DateTime.Today.AddDays(-61),
        },
        new()
        {
            Id          = "med-4",
            Name        = "Vitamin D3 1000IU",
            GenericName = "Cholecalciferol",
            Dosage      = "1000IU",
            Form        = "Capsule",
            Frequency   = "Once daily",
            Instructions= "Take with a meal",
            StartDate   = DateTime.Today.AddDays(-9),
            EndDate     = null,
            IsActive    = true,
            CreatedAt   = DateTime.Today.AddDays(-10),
        },
    };

    private readonly List<PrescriptionResponse> _prescriptions = new()
    {
        new()
        {
            Id               = "rx-2401",
            DoctorName       = "Dr Moosa, Lenasia South",
            Diagnosis        = "Type 2 Diabetes Management",
            PrescriptionDate = DateTime.Today.AddDays(-30),
            Notes            = "3-month repeat",
            CreatedAt        = DateTime.Today.AddDays(-30),
        },
        new()
        {
            Id               = "rx-2398",
            DoctorName       = "Dr Moosa, Lenasia South",
            Diagnosis        = "Hypertension",
            PrescriptionDate = DateTime.Today.AddDays(-20),
            Notes            = "5-month repeat",
            CreatedAt        = DateTime.Today.AddDays(-20),
        },
    };

    private readonly List<ReminderResponse> _reminders = new()
    {
        new()
        {
            Id           = "rem-1",
            MedicationId = "med-1",
            Time         = new TimeSpan(8, 0, 0),
            Days         = Enum.GetValues<DayOfWeek>().ToList(),
            IsEnabled    = true,
            CreatedAt    = DateTime.Today.AddDays(-30),
        },
        new()
        {
            Id           = "rem-2",
            MedicationId = "med-1",
            Time         = new TimeSpan(20, 0, 0),
            Days         = Enum.GetValues<DayOfWeek>().ToList(),
            IsEnabled    = true,
            CreatedAt    = DateTime.Today.AddDays(-30),
        },
        new()
        {
            Id           = "rem-3",
            MedicationId = "med-2",
            Time         = new TimeSpan(7, 30, 0),
            Days         = Enum.GetValues<DayOfWeek>().ToList(),
            IsEnabled    = true,
            CreatedAt    = DateTime.Today.AddDays(-20),
        },
    };

    private static readonly UserResponse _user = new()
    {
        Id          = "user-1",
        Email       = "ahmed@email.com",
        FirstName   = "Ahmed",
        LastName    = "Cassim",
        PhoneNumber = "072 000 0000",
        CreatedAt   = DateTime.Today.AddYears(-1),
    };

    // ── Auth ──────────────────────────────────────────────────────────────────

    public Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Accept any non-empty credentials
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Invalid credentials");

        return Task.FromResult(new AuthResponse
        {
            Token        = "stub-token-" + Guid.NewGuid(),
            RefreshToken = "stub-refresh-" + Guid.NewGuid(),
            User         = _user,
        });
    }

    public Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        return Task.FromResult(new AuthResponse
        {
            Token        = "stub-token-" + Guid.NewGuid(),
            RefreshToken = "stub-refresh-" + Guid.NewGuid(),
            User         = new UserResponse
            {
                Id          = "user-new",
                Email       = request.Email,
                FirstName   = request.FirstName,
                LastName    = request.LastName,
                PhoneNumber = request.PhoneNumber,
                CreatedAt   = DateTime.UtcNow,
            },
        });
    }

    public Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        return Task.FromResult(new AuthResponse
        {
            Token        = "stub-token-" + Guid.NewGuid(),
            RefreshToken = "stub-refresh-" + Guid.NewGuid(),
            User         = _user,
        });
    }

    // ── Medications ───────────────────────────────────────────────────────────

    public Task<List<MedicationResponse>> GetMedicationsAsync()
        => Task.FromResult(_medications.ToList());

    public Task<MedicationResponse> GetMedicationByIdAsync(string id)
    {
        var med = _medications.FirstOrDefault(m => m.Id == id)
            ?? throw new KeyNotFoundException($"Medication {id} not found");
        return Task.FromResult(med);
    }

    public Task<MedicationResponse> CreateMedicationAsync(CreateMedicationRequest request)
    {
        var med = new MedicationResponse
        {
            Id           = "med-" + (char)('a' + _medications.Count),
            Name         = request.Name,
            GenericName  = request.GenericName,
            Dosage       = request.Dosage,
            Form         = request.Form,
            Frequency    = request.Frequency,
            Instructions = request.Instructions,
            StartDate    = request.StartDate,
            EndDate      = request.EndDate,
            IsActive     = request.IsActive,
            CreatedAt    = DateTime.UtcNow,
        };
        _medications.Add(med);
        return Task.FromResult(med);
    }

    public Task<MedicationResponse> UpdateMedicationAsync(string id, UpdateMedicationRequest request)
    {
        var med = _medications.FirstOrDefault(m => m.Id == id)
            ?? throw new KeyNotFoundException($"Medication {id} not found");

        med.Name         = request.Name;
        med.GenericName  = request.GenericName;
        med.Dosage       = request.Dosage;
        med.Form         = request.Form;
        med.Frequency    = request.Frequency;
        med.Instructions = request.Instructions;
        med.StartDate    = request.StartDate;
        med.EndDate      = request.EndDate;
        med.IsActive     = request.IsActive;
        med.UpdatedAt    = DateTime.UtcNow;

        return Task.FromResult(med);
    }

    public Task DeleteMedicationAsync(string id)
    {
        var med = _medications.FirstOrDefault(m => m.Id == id);
        if (med != null) _medications.Remove(med);
        return Task.CompletedTask;
    }

    // ── Prescriptions ─────────────────────────────────────────────────────────

    public Task<List<PrescriptionResponse>> GetPrescriptionsAsync()
        => Task.FromResult(_prescriptions.ToList());

    public Task<PrescriptionResponse> GetPrescriptionByIdAsync(string id)
    {
        var rx = _prescriptions.FirstOrDefault(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Prescription {id} not found");
        return Task.FromResult(rx);
    }

    public Task<PrescriptionResponse> CreatePrescriptionAsync(CreatePrescriptionRequest request)
    {
        var rx = new PrescriptionResponse
        {
            Id               = "rx-" + (_prescriptions.Count + 2390),
            DoctorName       = string.IsNullOrEmpty(request.DoctorName) ? "Unknown Doctor" : request.DoctorName,
            Diagnosis        = request.Diagnosis,
            PrescriptionDate = request.PrescriptionDate,
            Notes            = request.Notes,
            CreatedAt        = DateTime.UtcNow,
        };
        _prescriptions.Add(rx);
        return Task.FromResult(rx);
    }

    public Task<PrescriptionResponse> UpdatePrescriptionAsync(string id, UpdatePrescriptionRequest request)
    {
        var rx = _prescriptions.FirstOrDefault(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Prescription {id} not found");

        if (!string.IsNullOrEmpty(request.DoctorName)) rx.DoctorName = request.DoctorName;
        if (!string.IsNullOrEmpty(request.Diagnosis))  rx.Diagnosis  = request.Diagnosis;
        if (!string.IsNullOrEmpty(request.Notes))      rx.Notes      = request.Notes;
        rx.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(rx);
    }

    public Task DeletePrescriptionAsync(string id)
    {
        var rx = _prescriptions.FirstOrDefault(p => p.Id == id);
        if (rx != null) _prescriptions.Remove(rx);
        return Task.CompletedTask;
    }

    // ── Reminders ─────────────────────────────────────────────────────────────

    public Task<List<ReminderResponse>> GetRemindersAsync()
        => Task.FromResult(_reminders.ToList());

    public Task<List<ReminderResponse>> GetRemindersByMedicationAsync(string medicationId)
        => Task.FromResult(_reminders.Where(r => r.MedicationId == medicationId).ToList());

    public Task<ReminderResponse> CreateReminderAsync(CreateReminderRequest request)
    {
        var reminder = new ReminderResponse
        {
            Id           = "rem-" + (_reminders.Count + 1),
            MedicationId = request.MedicationId,
            Time         = request.Time,
            Days         = request.Days ?? new List<DayOfWeek>(),
            IsEnabled    = true,
            CreatedAt    = DateTime.UtcNow,
        };
        _reminders.Add(reminder);
        return Task.FromResult(reminder);
    }

    public Task<ReminderResponse> UpdateReminderAsync(string id, UpdateReminderRequest request)
    {
        var reminder = _reminders.FirstOrDefault(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Reminder {id} not found");

        reminder.Time      = request.Time;
        reminder.Days      = request.Days ?? reminder.Days;
        reminder.IsEnabled = request.IsEnabled;
        reminder.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(reminder);
    }

    public Task DeleteReminderAsync(string id)
    {
        var reminder = _reminders.FirstOrDefault(r => r.Id == id);
        if (reminder != null) _reminders.Remove(reminder);
        return Task.CompletedTask;
    }

    // ── User Profile ──────────────────────────────────────────────────────────

    public Task<UserResponse> GetProfileAsync()
        => Task.FromResult(_user);

    public Task<UserResponse> UpdateProfileAsync(UpdateProfileRequest request)
    {
        if (!string.IsNullOrEmpty(request.FirstName))   _user.FirstName   = request.FirstName;
        if (!string.IsNullOrEmpty(request.LastName))    _user.LastName    = request.LastName;
        if (!string.IsNullOrEmpty(request.PhoneNumber)) _user.PhoneNumber = request.PhoneNumber;
        return Task.FromResult(_user);
    }
}
