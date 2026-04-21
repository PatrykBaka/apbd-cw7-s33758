using System.Data;
using cw7.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace cw7.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    
    private readonly IConfiguration _configuration;

    public AppointmentsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] string? status, string? patientLastName ,CancellationToken ct)
    {

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        await using var con = new SqlConnection(connectionString);
        
        await using var com = new SqlCommand();
        
        com.Connection = con;
        com.CommandText = @"
            SELECT
                a.IdAppointment,
                a.AppointmentDate,
                a.Status,
                a.Reason,
                p.FirstName + N' ' + p.LastName AS PatientFullName,
                p.Email AS PatientEmail
            FROM dbo.Appointments a
            JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
            WHERE (@Status IS NULL OR a.Status = @Status)
                AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
            ORDER BY a.AppointmentDate;";
        
        com.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        com.Parameters.AddWithValue("@PatientLastName", (object?)patientLastName ?? DBNull.Value);
        
        await con.OpenAsync(ct);
        
        await using SqlDataReader reader = await com.ExecuteReaderAsync(ct);

        var result = new List<AppointmentListDto>();
        while (await reader.ReadAsync(ct))
        {
            var app = new AppointmentListDto();
            app.IdAppointment = (int)reader["IdAppointment"];
            app.AppointmentDate = (DateTime)reader["AppointmentDate"];
            app.Status = (string)reader["Status"];
            app.Reason = (string)reader["Reason"];
            app.PatientFullName = (string)reader["PatientFullName"];
            app.PatientEmail = (string)reader["PatientEmail"];
            result.Add(app);
        }
        
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAsyncById(int id, CancellationToken ct)
    {
        
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        await using var con = new SqlConnection(connectionString);
        
        await using var com = new SqlCommand();
        
        com.Connection = con;

        com.CommandText = @"
             SELECT 
                 a.IdAppointment,
                 a.AppointmentDate,
                 a.Status,
                 a.Reason,
                 p.FirstName + N' ' + p.LastName AS PatientFullName,
                 p.Email AS PatientEmail
                 FROM dbo.Appointments a
                 JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                 WHERE a.IdAppointment = @id";
        
        com.Parameters.AddWithValue("@id", id);
        
        await con.OpenAsync(ct);
        
        await using SqlDataReader reader = await com.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
        {
            var app = new AppointmentListDto();
            app.IdAppointment = (int)reader["IdAppointment"];
            app.AppointmentDate = (DateTime)reader["AppointmentDate"];
            app.Status = (string)reader["Status"];
            app.Reason = (string)reader["Reason"];
            app.PatientFullName = (string)reader["PatientFullName"];
            app.PatientEmail = (string)reader["PatientEmail"];
            
            return Ok(app);
        }
        
        return NotFound("Appointment not found");
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] CreateAppointment ca, CancellationToken ct)
    {

        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        await using var con = new SqlConnection(connectionString);

        await using var com = new SqlCommand();
        
        com.Connection = con;
        
        await con.OpenAsync(ct);

        com.CommandText = "SELECT 1 FROM Patients WHERE IdPatient = @id AND IsActive = 1";
        com.Parameters.Clear();
        com.Parameters.AddWithValue("@id", ca.IdPatient);
        
        var result = await com.ExecuteScalarAsync(ct);
        if (result is null)
        {
            return BadRequest("Patient not found or is not active");
        }
        
        com.CommandText = "SELECT 1 FROM Doctors WHERE IdDoctor = @id AND IsActive = 1";
        com.Parameters.Clear();
        com.Parameters.AddWithValue("@id", ca.IdDoctor);
        
        result = await com.ExecuteScalarAsync(ct);
        if (result is null)
        {
            return BadRequest("Doctor not found or is not active");
        }

        if (ca.AppointmentDate < DateTime.Now)
        {
            return BadRequest("Appointment date has to be in the future");
        }

        if (ca.Reason.Length > 250)
        {
            return BadRequest("Reason is too long");
        }
        
        com.CommandText = "SELECT 1 FROM Appointments WHERE IdDoctor = @id AND AppointmentDate = @date AND Status = 'Scheduled'";
        com.Parameters.Clear();
        com.Parameters.AddWithValue("@id", ca.IdDoctor);
        com.Parameters.AddWithValue("@date", ca.AppointmentDate);

        result = await com.ExecuteScalarAsync(ct);
        if (result is not null)
        {
            return Conflict("Doctor already has appointment");
        }

        com.CommandText = @"INSERT INTO Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason, InternalNotes)
                            VALUES (@idPatient, @idDoctor, @appointmentDate, @status, @reason, @internalNotes);
                            SELECT CAST(SCOPE_IDENTITY() as int);";
        com.Parameters.Clear();
        com.Parameters.AddWithValue("@idPatient",  ca.IdPatient);
        com.Parameters.AddWithValue("@idDoctor",  ca.IdDoctor);
        com.Parameters.Add("@appointmentDate", SqlDbType.DateTime2).Value = ca.AppointmentDate;
        com.Parameters.AddWithValue("@status", ca.Status);
        com.Parameters.AddWithValue("@reason", ca.Reason);
        com.Parameters.AddWithValue("@internalNotes", (object?)ca.InternalNotes ?? DBNull.Value);

        var newId = (int)await com.ExecuteScalarAsync(ct);
        
        return CreatedAtAction(nameof(GetAsyncById), new { id = newId }, ca);
    }
    
    
}