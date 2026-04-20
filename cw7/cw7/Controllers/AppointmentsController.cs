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
    
    
    
}