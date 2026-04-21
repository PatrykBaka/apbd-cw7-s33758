using System.ComponentModel.DataAnnotations;

namespace cw7.DTO;

public class CreateAppointment
{
    
    public int IdPatient { get; set; }
    public int IdDoctor { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; }
    [Required]
    public string Reason { get; set; }

    public string? InternalNotes { get; set; }
    
}