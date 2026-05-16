# STEP 4: Create API Models

This step creates all request and response DTOs for the API.

## Create Directory Structure:

```bash
mkdir -p Models/Api/Requests
mkdir -p Models/Api/Responses
mkdir -p Models/Domain
```

## Create Models/Api/Requests/LoginRequest.cs

```csharp
namespace ScriptzApp.Models.Api.Requests;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

## Create Models/Api/Requests/RegisterRequest.cs

```csharp
namespace ScriptzApp.Models.Api.Requests;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
```

## Create Models/Api/Requests/RefreshTokenRequest.cs

```csharp
namespace ScriptzApp.Models.Api.Requests;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
```

## Create Models/Api/Requests/MedicationRequests.cs

```csharp
namespace ScriptzApp.Models.Api.Requests;

public class CreateMedicationRequest
{
    public string Name { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Form { get; set; } = string.Empty; // Tablet, Capsule, Liquid, etc.
    public string Frequency { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateMedicationRequest : CreateMedicationRequest
{
}
```

## Create Models/Api/Requests/PrescriptionRequests.cs

```csharp
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
```

## Create Models/Api/Requests/ReminderRequests.cs

```csharp
namespace ScriptzApp.Models.Api.Requests;

public class CreateReminderRequest
{
    public string MedicationId { get; set; } = string.Empty;
    public TimeSpan Time { get; set; }
    public List<DayOfWeek> Days { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

public class UpdateReminderRequest : CreateReminderRequest
{
}
```

## Create Models/Api/Requests/UpdateProfileRequest.cs

```csharp
namespace ScriptzApp.Models.Api.Requests;

public class UpdateProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
}
```

## Create Models/Api/Responses/AuthResponse.cs

```csharp
namespace ScriptzApp.Models.Api.Responses;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserResponse User { get; set; } = new();
}
```

## Create Models/Api/Responses/UserResponse.cs

```csharp
namespace ScriptzApp.Models.Api.Responses;

public class UserResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## Create Models/Api/Responses/MedicationResponse.cs

```csharp
namespace ScriptzApp.Models.Api.Responses;

public class MedicationResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Form { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

## Create Models/Api/Responses/PrescriptionResponse.cs

```csharp
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
```

## Create Models/Api/Responses/ReminderResponse.cs

```csharp
namespace ScriptzApp.Models.Api.Responses;

public class ReminderResponse
{
    public string Id { get; set; } = string.Empty;
    public string MedicationId { get; set; } = string.Empty;
    public MedicationResponse? Medication { get; set; }
    public TimeSpan Time { get; set; }
    public List<DayOfWeek> Days { get; set; } = new();
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**STOP HERE - Confirm all model files are created before proceeding to Step 5**
